namespace api.Models;

using Microsoft.EntityFrameworkCore;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Purchase> Purchases => Set<Purchase>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Purchase>(purchase =>
        {
            purchase.HasKey(p => p.Id);
            purchase.Property(p => p.Id).UseIdentityAlwaysColumn();
            purchase.Property(p => p.Description).HasMaxLength(50);
            purchase.Property(p => p.Amount).HasPrecision(12, 2);
            purchase.HasIndex(p => p.ClientPurchaseId).IsUnique();
        });
    }
}
