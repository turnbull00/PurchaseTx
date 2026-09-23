namespace api.Models;

using Microsoft.EntityFrameworkCore;

using Npgsql;

public class PostgresPurchaseRepository(AppDbContext dbContext) : IPurchaseRepository
{
    public async Task<IReadOnlyList<Purchase>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.Purchases.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<Purchase?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        return await dbContext.Purchases.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Purchase?> GetByClientPurchaseIdAsync(Guid clientPurchaseId, CancellationToken cancellationToken = default)
    {
        return await dbContext.Purchases.AsNoTracking().FirstOrDefaultAsync(p => p.ClientPurchaseId == clientPurchaseId, cancellationToken);
    }

    public async Task<Purchase> AddAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        var entry = dbContext.Purchases.Add(purchase);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Purchases_ClientPurchaseId" })
        {
            throw new DuplicateClientPurchaseIdException(purchase.ClientPurchaseId);
        }

        return entry.Entity;
    }
}
