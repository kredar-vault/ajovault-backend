using AjoVault.API.Auth;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using AjoVault.API.Payouts;
using AjoVault.API.Wallet.Dto;

namespace AjoVault.API.Wallet;

public class WalletService(
    UserRepository userRepo,
    GroupRepository groupRepo,
    ContributionRepository contributionRepo,
    PayoutRepository payoutRepo)
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

        return new VirtualAccountResponse
        {
            AccountNumber = user.BankAccountNumber,
            AccountName = user.BankAccountName,
            BankCode = user.BankCode,
            IsSet = user.BankAccountNumber != null
        };
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
