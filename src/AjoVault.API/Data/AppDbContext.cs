using AjoVault.API.Auth;
using AjoVault.API.Contributions;
using AjoVault.API.Groups;
using AjoVault.API.Notifications;
using AjoVault.API.Payouts;
using AjoVault.API.Wallet;
using Microsoft.EntityFrameworkCore;

namespace AjoVault.API.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<SavingsGroup> SavingsGroups => Set<SavingsGroup>();
    public DbSet<GroupMember> GroupMembers => Set<GroupMember>();
    public DbSet<Contribution> Contributions => Set<Contribution>();
    public DbSet<Payout> Payouts => Set<Payout>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Withdrawal> Withdrawals => Set<Withdrawal>();
    public DbSet<Deposit> Deposits => Set<Deposit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).IsRequired();
            e.Property(u => u.AccountNumber).HasMaxLength(20);
        });

        modelBuilder.Entity<SavingsGroup>(e =>
        {
            e.HasKey(g => g.Id);
            e.Property(g => g.Frequency).HasConversion<string>();
            e.Property(g => g.Status).HasConversion<string>();
            e.Property(g => g.ContributionAmount).HasPrecision(18, 2);
            e.HasIndex(g => g.InviteCode).IsUnique();
            e.Property(g => g.InviteCode).HasMaxLength(100);
            e.Property(g => g.DvaAccountNumber).HasMaxLength(20);
            e.Property(g => g.DvaBankName).HasMaxLength(100);
            e.Property(g => g.DvaAccountName).HasMaxLength(200);
        });

        modelBuilder.Entity<GroupMember>(e =>
        {
            e.HasKey(m => m.Id);
            e.HasIndex(m => new { m.GroupId, m.UserId }).IsUnique();
            e.Property(m => m.Role).HasConversion<string>();
        });

        modelBuilder.Entity<Notification>(e =>
        {
            e.HasKey(n => n.Id);
            e.HasIndex(n => n.UserId);
            e.Property(n => n.Title).HasMaxLength(200);
            e.Property(n => n.Type).HasMaxLength(50);
        });

        modelBuilder.Entity<Contribution>(e =>
        {
            e.HasKey(c => c.Id);
            e.HasIndex(c => new { c.GroupId, c.UserId, c.CycleNumber }).IsUnique();
            e.Property(c => c.Amount).HasPrecision(18, 2);
            e.Property(c => c.Status).HasConversion<string>();
            e.Property(c => c.Reference).HasMaxLength(20);
        });

        modelBuilder.Entity<Payout>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.GroupId, p.CycleNumber }).IsUnique();
            e.Property(p => p.Status).HasConversion<string>();
            e.Property(p => p.Amount).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Withdrawal>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.UserId);
            e.Property(w => w.Amount).HasPrecision(18, 2);
            e.Property(w => w.Status).HasMaxLength(20);
        });

        modelBuilder.Entity<Deposit>(e =>
        {
            e.HasKey(d => d.Id);
            e.HasIndex(d => d.UserId);
            e.HasIndex(d => d.Reference).IsUnique();
            e.Property(d => d.Amount).HasPrecision(18, 2);
            e.Property(d => d.Reference).HasMaxLength(100);
            e.Property(d => d.Source).HasMaxLength(50);
        });
    }
}
