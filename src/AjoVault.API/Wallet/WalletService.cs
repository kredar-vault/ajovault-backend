using AjoVault.API.Auth;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using AjoVault.API.Kredar;
using AjoVault.API.Payouts;
using AjoVault.API.Wallet.Dto;

namespace AjoVault.API.Wallet;

public class WalletService(
    UserRepository userRepo,
    GroupRepository groupRepo,
    ContributionRepository contributionRepo,
    PayoutRepository payoutRepo,
    WithdrawalRepository withdrawalRepo,
    DepositRepository depositRepo,
    KredarClient kredarClient,
    ILogger<WalletService> logger,
    IServiceScopeFactory scopeFactory)
{
    public async Task<WalletResponse> GetWalletAsync(Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var myGroups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = myGroups.Select(g => g.Id).ToList();

        var allContributions = await contributionRepo.GetAllByUserGroupsAsync(groupIds);
        var myContributions = allContributions.Where(c => c.UserId == userId && c.Status == ContributionStatus.Received);
        var totalOut = myContributions.Sum(c => c.Amount);

        var allPayouts = await payoutRepo.GetByGroupIdsAsync(groupIds);
        var receivedPayouts = allPayouts.Where(p => p.RecipientUserId == userId && p.Status == PayoutStatus.Disbursed);
        var totalIn = receivedPayouts.Sum(p => p.Amount);

        var totalWithdrawn = await withdrawalRepo.GetTotalWithdrawnAsync(userId);
        var totalDeposited = await depositRepo.GetTotalDepositedAsync(userId);

        return new WalletResponse
        {
            UserId = userId,
            Balance = totalDeposited + totalIn - totalOut - totalWithdrawn,
            TotalIn = totalDeposited + totalIn,
            TotalOut = totalOut + totalWithdrawn,
            Currency = "NGN",
            ActiveGroups = myGroups.Count(g => g.Status == GroupStatus.Active),
            TotalGroups = myGroups.Count,
            VirtualAccount = new VirtualAccountResponse
            {
                AccountNumber = user.BankAccountNumber,
                AccountName = user.BankAccountName,
                BankCode = user.BankCode,
                IsSet = user.BankAccountNumber != null
            }
        };
    }

    public async Task<VirtualAccountResponse> GetVirtualAccountAsync(Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.DvaAccountNumber == null)
        {
            await ProvisionDvaAsync(userId);
            user = await userRepo.FindByIdAsync(userId) ?? user;
        }

        return new VirtualAccountResponse
        {
            AccountNumber = user.DvaAccountNumber ?? user.BankAccountNumber,
            AccountName = user.DvaAccountName ?? user.BankAccountName,
            Bank = user.DvaBankName ?? user.BankCode,
            IsSet = user.DvaAccountNumber != null || user.BankAccountNumber != null
        };
    }

    private async Task ProvisionDvaAsync(Guid userId)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var repo = scope.ServiceProvider.GetRequiredService<UserRepository>();
            var kredar = scope.ServiceProvider.GetRequiredService<KredarClient>();

            var user = await repo.FindByIdAsync(userId);
            if (user == null || user.DvaAccountNumber != null) return;

            var nameParts = user.FullName.Trim().Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 && !string.IsNullOrWhiteSpace(nameParts[1]) ? nameParts[1].Trim() : "User";
            var customer = await kredar.CreateOrGetCustomerAsync(firstName, lastName, user.Email, user.PhoneNumber);
            if (customer == null)
            {
                logger.LogError("Kredar returned null customer for user {UserId} ({Email}) — create and lookup both failed", userId, user.Email);
                return;
            }

            var dva = await kredar.CreateOrGetDvaAsync(customer.Id, null);
            if (dva == null)
            {
                logger.LogError("Kredar returned null DVA for customer {CustomerId} (user {UserId}) — create and lookup both failed", customer.Id, userId);
                return;
            }

            user.KredarCustomerId = customer.Id;
            user.DvaAccountNumber = dva.AccountNumber;
            user.DvaBankName = dva.BankName;
            user.DvaAccountName = dva.AccountName;
            await repo.UpdateAsync(user);

            logger.LogInformation("Personal DVA {AccountNumber} provisioned for user {UserId}", dva.AccountNumber, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision DVA for user {UserId}", userId);
        }
    }

    public async Task<WithdrawalResponse> WithdrawAsync(Guid userId, decimal amount)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (amount <= 0)
            throw new InvalidOperationException("Amount must be greater than zero.");

        if (string.IsNullOrWhiteSpace(user.BankAccountNumber) || string.IsNullOrWhiteSpace(user.BankCode))
            throw new InvalidOperationException("Please add your bank account details in your profile before withdrawing.");

        var myGroups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = myGroups.Select(g => g.Id).ToList();
        var allContributions = await contributionRepo.GetAllByUserGroupsAsync(groupIds);
        var totalOut = allContributions.Where(c => c.UserId == userId && c.Status == ContributionStatus.Received).Sum(c => c.Amount);
        var allPayouts = await payoutRepo.GetByGroupIdsAsync(groupIds);
        var totalIn = allPayouts.Where(p => p.RecipientUserId == userId && p.Status == PayoutStatus.Disbursed).Sum(p => p.Amount);
        var totalWithdrawn = await withdrawalRepo.GetTotalWithdrawnAsync(userId);
        var totalDeposited = await depositRepo.GetTotalDepositedAsync(userId);
        var balance = totalDeposited + totalIn - totalOut - totalWithdrawn;

        if (amount > balance)
            throw new InvalidOperationException($"Insufficient balance. Available: ₦{balance:N2}");

        var withdrawal = new Withdrawal { UserId = userId, Amount = amount, Status = "Pending" };
        await withdrawalRepo.AddAsync(withdrawal);

        var txRef = $"ajovault-wd-{withdrawal.Id:N}";
        try
        {
            var result = await kredarClient.InitiateTransferAsync(
                txRef, amount, user.BankAccountNumber, user.BankCode,
                $"AjoVault wallet withdrawal");

            withdrawal.Status = result.Status == "Succeeded" ? "Completed" : "Failed";
            withdrawal.KredarReference = result.ProviderReference ?? txRef;
            withdrawal.FailureReason = result.FailureReason;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Kredar transfer failed for withdrawal {WithdrawalId}", withdrawal.Id);
            withdrawal.Status = "Failed";
            withdrawal.FailureReason = ex.Message;
        }

        await withdrawalRepo.UpdateAsync(withdrawal);

        if (withdrawal.Status == "Failed")
            throw new InvalidOperationException($"Transfer failed: {withdrawal.FailureReason ?? "Unknown error"}. Please try again.");

        return new WithdrawalResponse
        {
            Id = withdrawal.Id,
            Amount = withdrawal.Amount,
            AccountNumber = user.BankAccountNumber,
            AccountName = user.BankAccountName ?? user.FullName,
            BankName = user.BankCode,
            Status = withdrawal.Status,
            CreatedAt = withdrawal.CreatedAt
        };
    }

    public async Task<BankLookupResponse> LookupBankAsync(string accountNumber, string bankCode, CancellationToken ct = default)
    {
        var result = await kredarClient.LookupBankAccountAsync(accountNumber.Trim(), bankCode.Trim(), ct)
            ?? throw new InvalidOperationException("Could not verify account. Check the account number and bank code, then try again.");

        return new BankLookupResponse
        {
            AccountName = result.AccountName,
            AccountNumber = result.AccountNumber,
            BankCode = result.BankCode,
        };
    }

    public async Task<VirtualAccountResponse> SetBankAccountAsync(Guid userId, CreateVirtualAccountRequest request, CancellationToken ct = default)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var accountNumber = request.AccountNumber.Trim();
        var bankCode = request.BankCode.Trim();

        // Verify the account exists and get the official name from the bank
        var lookup = await kredarClient.LookupBankAccountAsync(accountNumber, bankCode, ct);
        var verifiedName = lookup?.AccountName ?? request.AccountName.Trim();

        user.BankAccountNumber = accountNumber;
        user.BankAccountName = verifiedName;
        user.BankCode = bankCode;
        await userRepo.UpdateAsync(user);

        return new VirtualAccountResponse
        {
            AccountNumber = user.BankAccountNumber,
            AccountName = user.BankAccountName,
            BankCode = user.BankCode,
            IsSet = true
        };
    }
}
