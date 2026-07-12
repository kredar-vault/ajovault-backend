using AjoVault.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Wallet;

public class WithdrawalRepository(AppDbContext db)
{
    public async Task<Withdrawal> AddAsync(Withdrawal withdrawal)
    {
        db.Withdrawals.Add(withdrawal);
        await db.SaveChangesAsync();
        return withdrawal;
    }

    public async Task<List<Withdrawal>> GetByUserAsync(Guid userId)
        => await db.Withdrawals
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync();

    public async Task<decimal> GetTotalWithdrawnAsync(Guid userId)
        => await db.Withdrawals
            .Where(w => w.UserId == userId)
            .SumAsync(w => (decimal?)w.Amount) ?? 0;
}
