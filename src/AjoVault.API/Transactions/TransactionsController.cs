using AjoVault.API.Auth;
using AjoVault.API.Common;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using AjoVault.API.Payouts;
using AjoVault.API.Wallet;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AjoVault.API.Transactions;

public record TransactionEntry(Guid Id, string Type, string Description, decimal Amount, string Direction, Guid GroupId, string GroupName, int? CycleNumber, string? Reference, DateTime OccurredAt, string Status);

[ApiController]
[Route("api/v1/transactions")]
[Authorize]
public class TransactionsController(
    GroupRepository groupRepo,
    ContributionRepository contributionRepo,
    PayoutRepository payoutRepo,
    WithdrawalRepository withdrawalRepo,
    DepositRepository depositRepo) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = UserContext.GetUserId(HttpContext);
        var entries = await BuildTransactionsAsync(userId);
        return Ok(ApiResponse<List<TransactionEntry>>.Success(entries));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var userId = UserContext.GetUserId(HttpContext);
        var entries = await BuildTransactionsAsync(userId);
        var entry = entries.FirstOrDefault(e => e.Id == id)
            ?? throw new KeyNotFoundException("Transaction not found.");
        return Ok(ApiResponse<TransactionEntry>.Success(entry));
    }

    private async Task<List<TransactionEntry>> BuildTransactionsAsync(Guid userId)
    {
        var myGroups = await groupRepo.GetByMemberAsync(userId);
        var groupIds = myGroups.Select(g => g.Id).ToList();
        var groupLookup = myGroups.ToDictionary(g => g.Id, g => g.Name);

        var allContributions = await contributionRepo.GetAllByUserGroupsAsync(groupIds);
        var myContributions = allContributions.Where(c => c.UserId == userId && c.Status == ContributionStatus.Received);

        var allPayouts = await payoutRepo.GetByGroupIdsAsync(groupIds);
        var myPayouts = allPayouts.Where(p => p.RecipientUserId == userId && p.Status == PayoutStatus.Disbursed);

        var withdrawals = await withdrawalRepo.GetByUserAsync(userId);
        var deposits = await depositRepo.GetByUserAsync(userId);

        var entries = new List<TransactionEntry>();

        foreach (var d in deposits)
            entries.Add(new TransactionEntry(d.Id, "Deposit",
                $"Wallet deposit — {d.Source}",
                d.Amount, "In", Guid.Empty, "", null, d.Reference, d.CreatedAt, "Completed"));

        foreach (var c in myContributions)
            entries.Add(new TransactionEntry(c.Id, "Contribution",
                $"{groupLookup.GetValueOrDefault(c.GroupId, "Group")} — Cycle {c.CycleNumber} contribution",
                c.Amount, "Out", c.GroupId, groupLookup.GetValueOrDefault(c.GroupId, ""),
                c.CycleNumber, c.Reference, c.PaidAt, "Completed"));

        foreach (var p in myPayouts)
            entries.Add(new TransactionEntry(p.Id, "Payout",
                $"{groupLookup.GetValueOrDefault(p.GroupId, "Group")} — Cycle {p.CycleNumber} payout",
                p.Amount, "In", p.GroupId, groupLookup.GetValueOrDefault(p.GroupId, ""),
                p.CycleNumber, null, p.DisbursedAt ?? p.ScheduledDate, "Completed"));

        foreach (var w in withdrawals)
            entries.Add(new TransactionEntry(w.Id, "Withdrawal",
                "Withdrawal",
                w.Amount, "Out", Guid.Empty, "", null, null, w.CreatedAt, w.Status));

        return entries.OrderByDescending(e => e.OccurredAt).ToList();
    }
}
