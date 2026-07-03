using AjoVault.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Contributions;

public class ContributionRepository(AppDbContext db)
{
    public async Task<List<Contribution>> GetByGroupAsync(Guid groupId) =>
        await db.Contributions
            .Where(c => c.GroupId == groupId)
            .OrderByDescending(c => c.PaidAt)
            .ToListAsync();

    public async Task<List<Contribution>> GetByUserAsync(Guid groupId, Guid userId) =>
        await db.Contributions
            .Where(c => c.GroupId == groupId && c.UserId == userId)
            .OrderByDescending(c => c.PaidAt)
            .ToListAsync();

    public async Task<Contribution?> FindAsync(Guid groupId, Guid userId, int cycleNumber) =>
        await db.Contributions.FirstOrDefaultAsync(c =>
            c.GroupId == groupId && c.UserId == userId && c.CycleNumber == cycleNumber);

    public async Task AddAsync(Contribution contribution)
    {
        await db.Contributions.AddAsync(contribution);
        await db.SaveChangesAsync();
    }
}
