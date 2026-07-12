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
    KredarClient kredarClient,
    ILogger<WalletService> logger)
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

        return new WalletResponse
        {
            UserId = userId,
            Balance = totalIn - totalOut,
            TotalIn = totalIn,
            TotalOut = totalOut,
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

        // Auto-provision DVA for existing users who don't have one yet
        if (user.DvaAccountNumber == null)
            _ = Task.Run(() => ProvisionDvaAsync(user));

        return new VirtualAccountResponse
        {
            AccountNumber = user.DvaAccountNumber ?? user.BankAccountNumber,
            AccountName = user.DvaAccountName ?? user.BankAccountName,
            Bank = user.DvaBankName ?? user.BankCode,
            IsSet = user.DvaAccountNumber != null || user.BankAccountNumber != null
        };
    }

    private async Task ProvisionDvaAsync(User user)
    {
        try
        {
            if (user.KredarCustomerId.HasValue) return;

            var nameParts = user.FullName.Split(' ', 2);
            var customer = await kredarClient.CreateCustomerAsync(
                nameParts[0], nameParts.Length > 1 ? nameParts[1] : "User",
                user.Email, user.PhoneNumber);
            if (customer == null) return;

            var dva = await kredarClient.CreateDvaAsync(customer.Id, null);
            if (dva == null) return;

            user.KredarCustomerId = customer.Id;
            user.DvaAccountNumber = dva.AccountNumber;
            user.DvaBankName = dva.BankName;
            user.DvaAccountName = dva.AccountName;
            await userRepo.UpdateAsync(user);

            logger.LogInformation("Personal DVA {AccountNumber} provisioned for user {UserId}", dva.AccountNumber, user.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to provision DVA for user {UserId}", user.Id);
        }
    }

    public async Task<VirtualAccountResponse> SetBankAccountAsync(Guid userId, CreateVirtualAccountRequest request)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        user.BankAccountNumber = request.AccountNumber.Trim();
        user.BankAccountName = request.AccountName.Trim();
        user.BankCode = request.BankCode.Trim();
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
