using AjoVault.API.Groups;
using AjoVault.API.Payouts.Dto;

namespace AjoVault.API.Payouts;

public class PayoutsService(PayoutRepository payoutRepo, GroupRepository groupRepo)
{
    public async Task<List<PayoutResponse>> GetByGroupAsync(Guid groupId) =>
        (await payoutRepo.GetByGroupAsync(groupId)).Select(MapToResponse).ToList();

    public async Task<PayoutResponse> DisburseAsync(Guid userId, Guid groupId, Guid payoutId)
    {
        var group = await groupRepo.FindByIdAsync(groupId)
            ?? throw new KeyNotFoundException("Savings group not found.");

        if (group.CreatedByUserId != userId)
            throw new UnauthorizedAccessException("Only the group creator can disburse payouts.");

        var payout = await payoutRepo.FindByIdAsync(groupId, payoutId)
            ?? throw new KeyNotFoundException("Payout not found.");

        if (payout.Status == PayoutStatus.Disbursed)
            throw new InvalidOperationException("This payout has already been disbursed.");

        payout.Status = PayoutStatus.Disbursed;
        payout.DisbursedAt = DateTime.UtcNow;
        await payoutRepo.UpdateAsync(payout);

        return MapToResponse(payout);
    }

    private static PayoutResponse MapToResponse(Payout p) => new()
    {
        Id = p.Id,
        GroupId = p.GroupId,
        CycleNumber = p.CycleNumber,
        RecipientUserId = p.RecipientUserId,
        Amount = p.Amount,
        ScheduledDate = p.ScheduledDate,
        Status = p.Status.ToString(),
        DisbursedAt = p.DisbursedAt
    };
}
