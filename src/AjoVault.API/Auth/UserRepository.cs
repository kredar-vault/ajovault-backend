using AjoVault.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Auth;

public class UserRepository(AppDbContext db)
{
    public async Task<User?> FindByEmailAsync(string email) =>
        await db.Users.FirstOrDefaultAsync(u => u.Email == email);

    public async Task<User?> FindByIdAsync(Guid id) =>
        await db.Users.FindAsync(id);

    public async Task<List<User>> FindByIdsAsync(IEnumerable<Guid> ids) =>
        await db.Users.Where(u => ids.Contains(u.Id)).ToListAsync();

    public async Task AddAsync(User user)
    {
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }
}
