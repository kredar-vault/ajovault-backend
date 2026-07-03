using AjoVault.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Payouts;

public class PayoutRepository(AppDbContext db)
{
    public async Task<List<Payout>> GetByGroupAsync(Guid groupId) =>
        await db.Payouts
            .Where(p => p.GroupId == groupId)
            .OrderBy(p => p.CycleNumber)
            .ToListAsync();

    public async Task<Payout?> FindByIdAsync(Guid groupId, Guid payoutId) =>
        await db.Payouts.FirstOrDefaultAsync(p => p.GroupId == groupId && p.Id == payoutId);

    public async Task AddRangeAsync(IEnumerable<Payout> payouts)
    {
        await db.Payouts.AddRangeAsync(payouts);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Payout payout)
    {
        db.Payouts.Update(payout);
        await db.SaveChangesAsync();
    }
}
