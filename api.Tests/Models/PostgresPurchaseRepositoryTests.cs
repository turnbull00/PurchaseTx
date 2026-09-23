namespace api.Tests.Models;

using api.Models;

using DotNetEnv;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

using Npgsql;

public class PostgresPurchaseRepositoryTests : IAsyncLifetime
{
    private AppDbContext _dbContext = null!;
    private IDbContextTransaction _transaction = null!;

    public async Task InitializeAsync()
    {
        Env.TraversePath().Load();

        var connectionStringBuilder = new NpgsqlConnectionStringBuilder
        {
            Host = "localhost",
            Port = 5432,
            Database = "purchasetx",
            Username = "api",
            Password = Environment.GetEnvironmentVariable("Postgres__Password")
                ?? throw new InvalidOperationException("Postgres__Password is not set (expected in .env)."),
            SslMode = SslMode.Disable,
        };

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionStringBuilder.ConnectionString);

        _dbContext = new AppDbContext(optionsBuilder.Options);
        _transaction = await _dbContext.Database.BeginTransactionAsync();
    }

    public async Task DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _dbContext.DisposeAsync();
    }

    [Fact]
    public async Task AddAsync_persists_and_returns_purchase_with_generated_id()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);
        var purchase = new Purchase(default, "Coffee", DateTime.UtcNow, 4.50m, Guid.NewGuid());

        var saved = await repository.AddAsync(purchase);

        Assert.True(saved.Id > 0);
    }

    [Fact]
    public async Task AddAsync_throws_for_duplicate_ClientPurchaseId()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);
        var clientPurchaseId = Guid.NewGuid();
        var first = new Purchase(default, "Coffee", DateTime.UtcNow, 4.50m, clientPurchaseId);
        var duplicate = new Purchase(default, "Tea", DateTime.UtcNow, 3.00m, clientPurchaseId);

        await repository.AddAsync(first);

        await Assert.ThrowsAsync<DuplicateClientPurchaseIdException>(() => repository.AddAsync(duplicate));
    }

    [Fact]
    public async Task GetAllAsync_returns_inserted_purchases()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);
        var purchase = new Purchase(default, "Coffee", DateTime.UtcNow, 4.50m, Guid.NewGuid());
        await repository.AddAsync(purchase);

        var all = await repository.GetAllAsync();

        Assert.Contains(all, p => p.ClientPurchaseId == purchase.ClientPurchaseId);
    }

    [Fact]
    public async Task GetByIdAsync_returns_the_matching_purchase()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);
        var purchase = new Purchase(default, "Coffee", DateTime.UtcNow, 4.50m, Guid.NewGuid());
        var saved = await repository.AddAsync(purchase);

        var found = await repository.GetByIdAsync(saved.Id);

        Assert.NotNull(found);
        Assert.Equal(saved.Id, found.Id);
    }

    [Fact]
    public async Task GetByIdAsync_returns_null_when_no_match()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);

        var found = await repository.GetByIdAsync(long.MaxValue);

        Assert.Null(found);
    }

    [Fact]
    public async Task GetByClientPurchaseIdAsync_returns_the_matching_purchase()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);
        var clientPurchaseId = Guid.NewGuid();
        var purchase = new Purchase(default, "Coffee", DateTime.UtcNow, 4.50m, clientPurchaseId);
        await repository.AddAsync(purchase);

        var found = await repository.GetByClientPurchaseIdAsync(clientPurchaseId);

        Assert.NotNull(found);
        Assert.Equal(clientPurchaseId, found.ClientPurchaseId);
    }

    [Fact]
    public async Task GetByClientPurchaseIdAsync_returns_null_when_no_match()
    {
        var repository = new PostgresPurchaseRepository(_dbContext);

        var found = await repository.GetByClientPurchaseIdAsync(Guid.NewGuid());

        Assert.Null(found);
    }
}
