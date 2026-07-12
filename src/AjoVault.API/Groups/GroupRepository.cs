using AjoVault.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Groups;

public class GroupRepository(AppDbContext db)
{
    public async Task<List<SavingsGroup>> GetAllAsync() =>
        await db.SavingsGroups.OrderByDescending(g => g.CreatedAt).ToListAsync();

    public async Task<SavingsGroup?> FindByIdAsync(Guid id) =>
        await db.SavingsGroups.FirstOrDefaultAsync(g => g.Id == id);

    public async Task AddAsync(SavingsGroup group)
    {
        await db.SavingsGroups.AddAsync(group);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(SavingsGroup group)
    {
        db.SavingsGroups.Update(group);
        await db.SaveChangesAsync();
    }

    public async Task<List<GroupMember>> GetMembersAsync(Guid groupId) =>
        await db.GroupMembers
            .Where(m => m.GroupId == groupId)
            .OrderBy(m => m.PayoutPosition)
            .ToListAsync();

    public async Task<GroupMember?> FindMemberAsync(Guid groupId, Guid userId) =>
        await db.GroupMembers.FirstOrDefaultAsync(m => m.GroupId == groupId && m.UserId == userId);

    public async Task AddMemberAsync(GroupMember member)
    {
        await db.GroupMembers.AddAsync(member);
        await db.SaveChangesAsync();
    }

    public async Task<List<SavingsGroup>> GetByMemberAsync(Guid userId)
    {
        var groupIds = await db.GroupMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.GroupId)
            .ToListAsync();

        return await db.SavingsGroups
            .Where(g => groupIds.Contains(g.Id))
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync();
    }

    public async Task<SavingsGroup?> FindByInviteCodeAsync(string inviteCode) =>
        await db.SavingsGroups.FirstOrDefaultAsync(g => g.InviteCode == inviteCode);

    public async Task<SavingsGroup?> FindByDvaAccountNumberAsync(string accountNumber) =>
        await db.SavingsGroups.FirstOrDefaultAsync(g => g.DvaAccountNumber == accountNumber);

    public async Task DeleteMemberAsync(GroupMember member)
    {
        db.GroupMembers.Remove(member);
        await db.SaveChangesAsync();
    }

    public async Task UpdateMemberAsync(GroupMember member)
    {
        db.GroupMembers.Update(member);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(SavingsGroup group)
    {
        var members = await db.GroupMembers.Where(m => m.GroupId == group.Id).ToListAsync();
        var contributions = await db.Contributions.Where(c => c.GroupId == group.Id).ToListAsync();
        var payouts = await db.Payouts.Where(p => p.GroupId == group.Id).ToListAsync();

        db.GroupMembers.RemoveRange(members);
        db.Contributions.RemoveRange(contributions);
        db.Payouts.RemoveRange(payouts);
        db.SavingsGroups.Remove(group);
        await db.SaveChangesAsync();
    }
}
