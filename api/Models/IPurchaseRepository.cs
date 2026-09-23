namespace api.Models;

public interface IPurchaseRepository
{
    Task<IReadOnlyList<Purchase>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Purchase?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<Purchase?> GetByClientPurchaseIdAsync(Guid clientPurchaseId, CancellationToken cancellationToken = default);

    Task<Purchase> AddAsync(Purchase purchase, CancellationToken cancellationToken = default);
}
