using AjoVault.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Wallet;

public class DepositRepository(AppDbContext db)
{
    public async Task<Deposit> AddAsync(Deposit deposit)
    {
        db.Deposits.Add(deposit);
        await db.SaveChangesAsync();
        return deposit;
    }

    public async Task<List<Deposit>> GetByUserAsync(Guid userId)
        => await db.Deposits
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

    public async Task<decimal> GetTotalDepositedAsync(Guid userId)
        => await db.Deposits
            .Where(d => d.UserId == userId)
            .SumAsync(d => (decimal?)d.Amount) ?? 0;

    public async Task<bool> ExistsByReferenceAsync(string reference)
        => await db.Deposits.AnyAsync(d => d.Reference == reference);
}
