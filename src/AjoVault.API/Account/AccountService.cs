using AjoVault.API.Account.Dto;
using AjoVault.API.Auth;
using AjoVault.API.Groups;
using AjoVault.API.Payouts;

namespace AjoVault.API.Account;

public class AccountService(
    UserRepository userRepo,
    PayoutRepository payoutRepo,
    GroupRepository groupRepo)
{
    public async Task<AccountResponse> GetAsync(Guid userId)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        var disbursedPayouts = await payoutRepo.GetUpcomingByUserAsync(userId);
        // Balance = sum of payouts received (disbursed to this user)
        var allUserPayouts = (await payoutRepo.GetUpcomingByUserAsync(userId)).ToList();

        var myGroups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = myGroups.Select(g => g.Id).ToList();
        var groupLookup = myGroups.ToDictionary(g => g.Id, g => g.Name);

        var allPayouts = await payoutRepo.GetByGroupIdsAsync(groupIds);
        var receivedPayouts = allPayouts
            .Where(p => p.RecipientUserId == userId && p.Status == PayoutStatus.Disbursed)
            .ToList();

        var balance = receivedPayouts.Sum(p => p.Amount);

        var now = DateTime.UtcNow;
        var sixMonthsAgo = now.AddMonths(-5);
        var points = Enumerable.Range(0, 6)
            .Select(i => now.AddMonths(-5 + i))
            .Select(d => new MonthlyPoint
            {
                Month = d.ToString("MMM"),
                In = receivedPayouts
                    .Where(p => p.DisbursedAt.HasValue &&
                                p.DisbursedAt.Value.Month == d.Month &&
                                p.DisbursedAt.Value.Year == d.Year)
                    .Sum(p => p.Amount),
                Out = 0
            }).ToList();

        return new AccountResponse
        {
            UserId = user.Id,
            FullName = user.FullName,
            AccountNumber = user.AccountNumber,
            MaskedAccountNumber = MaskAccountNumber(user.AccountNumber),
            Balance = balance,
            MonthlySummary = new MonthlySummary
            {
                TotalIn = receivedPayouts.Sum(p => p.Amount),
                TotalOut = 0,
                Points = points
            }
        };
    }

    public async Task<List<TransactionResponse>> GetTransactionsAsync(Guid userId)
    {
        var myGroups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = myGroups.Select(g => g.Id).ToList();
        var groupLookup = myGroups.ToDictionary(g => g.Id, g => g.Name);

        var allPayouts = await payoutRepo.GetByGroupIdsAsync(groupIds);
        var receivedPayouts = allPayouts
            .Where(p => p.RecipientUserId == userId && p.Status == PayoutStatus.Disbursed)
            .OrderByDescending(p => p.DisbursedAt)
            .ToList();

        return receivedPayouts.Select(p => new TransactionResponse
        {
            Id = p.Id,
            Type = "RotationPayout",
            Description = $"{groupLookup.GetValueOrDefault(p.GroupId, "Group")} Rotation Payout",
            Amount = p.Amount,
            Direction = "In",
            OccurredAt = p.DisbursedAt ?? p.ScheduledDate
        }).ToList();
    }

    public async Task<AccountResponse> UpdateAsync(Guid userId, UpdateAccountRequest request)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!string.IsNullOrWhiteSpace(request.FullName)) user.FullName = request.FullName.Trim();
        if (!string.IsNullOrWhiteSpace(request.PhoneNumber)) user.PhoneNumber = request.PhoneNumber.Trim();
        await userRepo.UpdateAsync(user);

        return await GetAsync(userId);
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("New passwords do not match.");

        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 10);
        await userRepo.UpdateAsync(user);
    }

    public async Task ChangePinAsync(Guid userId, ChangePinRequest request)
    {
        var user = await userRepo.FindByIdAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.PinHash != null && !string.IsNullOrWhiteSpace(request.CurrentPin))
        {
            if (!BCrypt.Net.BCrypt.Verify(request.CurrentPin, user.PinHash))
                throw new UnauthorizedAccessException("Current PIN is incorrect.");
        }

        user.PinHash = BCrypt.Net.BCrypt.HashPassword(request.NewPin, workFactor: 6);
        await userRepo.UpdateAsync(user);
    }

    private static string MaskAccountNumber(string accountNumber)
    {
        if (accountNumber.Length <= 4)
            return accountNumber;
        return $"**** {accountNumber[^4..]}";
    }
}
