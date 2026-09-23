namespace api.Tests.Endpoints;

using api.Models;

internal class FakePurchaseRepository : IPurchaseRepository
{
    private readonly List<Purchase> _purchases = [];
    private long _nextId = 1;

    public Task<IReadOnlyList<Purchase>> GetAllAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<Purchase>>(_purchases);

    public Task<Purchase?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
        => Task.FromResult(_purchases.FirstOrDefault(p => p.Id == id));

    public Task<Purchase?> GetByClientPurchaseIdAsync(Guid clientPurchaseId, CancellationToken cancellationToken = default)
        => Task.FromResult(_purchases.FirstOrDefault(p => p.ClientPurchaseId == clientPurchaseId));

    public Task<Purchase> AddAsync(Purchase purchase, CancellationToken cancellationToken = default)
    {
        if (_purchases.Any(p => p.ClientPurchaseId == purchase.ClientPurchaseId))
        {
            throw new DuplicateClientPurchaseIdException(purchase.ClientPurchaseId);
        }

        var stored = purchase with { Id = _nextId++ };
        _purchases.Add(stored);
        return Task.FromResult(stored);
    }
}
