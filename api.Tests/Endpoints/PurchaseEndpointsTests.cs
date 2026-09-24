namespace api.Tests.Endpoints;

using api.Endpoints;
using api.Models;
using api.Services.ExchangeRates;

public class PurchaseEndpointsTests
{
    /*
    [Fact]
    public async Task GetPurchases_returns_empty_list_when_none_exist()
    {
        var repository = new FakePurchaseRepository();

        var result = await PurchaseEndpoints.GetPurchases(repository, CancellationToken.None);

        Assert.Empty(result.Value!);
    }
    */

    [Fact]
    public async Task PostPurchases_creates_and_returns_the_purchase_with_a_generated_id()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21T00:00:00Z", 4.50m, Guid.NewGuid());

        var result = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);

        var created = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(result.Result);
        Assert.True(created.Value!.Id > 0);
        Assert.Equal("Coffee", created.Value.Description);
        Assert.Equal($"/purchase/{created.Value.Id}", created.Location);
    }

    [Fact]
    public async Task PostPurchases_returns_conflict_for_a_duplicate_ClientPurchaseId()
    {
        var repository = new FakePurchaseRepository();
        var clientPurchaseId = Guid.NewGuid();
        var first = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21T00:00:00Z", 4.50m, clientPurchaseId);
        var duplicate = new PurchaseEndpoints.NewPurchase("Tea", "2026-09-21T00:00:00Z", 3.00m, clientPurchaseId);

        await PurchaseEndpoints.PostPurchases(first, repository, CancellationToken.None);
        var result = await PurchaseEndpoints.PostPurchases(duplicate, repository, CancellationToken.None);

        var conflict = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Conflict<string>>(result.Result);
        Assert.Contains(clientPurchaseId.ToString(), conflict.Value);
    }

    [Fact]
    public async Task GetPurchaseById_finds_by_database_id()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21T00:00:00Z", 4.50m, Guid.NewGuid());
        var created = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);
        var createdPurchase = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(created.Result).Value!;

        var result = await PurchaseEndpoints.GetPurchaseById(createdPurchase.Id.ToString(), null, repository, new ExchangeRateStore(), CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<PurchaseEndpoints.PurchaseResponse>>(result.Result);
        Assert.Equal(createdPurchase.Id, ok.Value!.Id);
    }

    [Fact]
    public async Task GetPurchaseById_finds_by_ClientPurchaseId()
    {
        var repository = new FakePurchaseRepository();
        var clientPurchaseId = Guid.NewGuid();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21T00:00:00Z", 4.50m, clientPurchaseId);
        await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);

        var result = await PurchaseEndpoints.GetPurchaseById(clientPurchaseId.ToString(), null, repository, new ExchangeRateStore(), CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<PurchaseEndpoints.PurchaseResponse>>(result.Result);
        Assert.Equal(clientPurchaseId, ok.Value!.ClientPurchaseId);
    }

    [Theory]
    [InlineData("999999")]
    [InlineData("00000000-0000-0000-0000-000000000000")]
    [InlineData("not-a-valid-id")]
    public async Task GetPurchaseById_returns_not_found_when_no_match_or_unparseable(string id)
    {
        var repository = new FakePurchaseRepository();

        var result = await PurchaseEndpoints.GetPurchaseById(id, null, repository, new ExchangeRateStore(), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.NotFound>(result.Result);
    }

    [Fact]
    public async Task GetPurchaseById_converts_amount_when_currency_and_rate_are_available()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-03-15T00:00:00Z", 10.00m, Guid.NewGuid());
        var created = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);
        var createdPurchase = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(created.Result).Value!;

        var exchangeRateStore = new ExchangeRateStore();
        exchangeRateStore.SetRates([
            new ExchangeRate(new DateOnly(2026, 3, 1), "France", "Euro", "France-Euro", new DateOnly(2026, 3, 1), 0.9m),
        ]);

        var result = await PurchaseEndpoints.GetPurchaseById(
            createdPurchase.Id.ToString(), "France-Euro", repository, exchangeRateStore, CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<PurchaseEndpoints.PurchaseResponse>>(result.Result);
        Assert.Equal("France-Euro", ok.Value!.Currency);
        Assert.Equal(0.9m, ok.Value.ExchangeRate);
        Assert.Equal(9.00m, ok.Value.ConvertedAmount);
    }

    [Fact]
    public async Task GetPurchaseById_rounds_a_half_cent_conversion_away_from_zero()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-03-15T00:00:00Z", 5.00m, Guid.NewGuid());
        var created = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);
        var createdPurchase = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(created.Result).Value!;

        var exchangeRateStore = new ExchangeRateStore();
        exchangeRateStore.SetRates([
            // 5.00 * 0.025 = 0.125 exactly; Math.Round's default (ToEven) would give 0.12.
            new ExchangeRate(new DateOnly(2026, 3, 1), "France", "Euro", "France-Euro", new DateOnly(2026, 3, 1), 0.025m),
        ]);

        var result = await PurchaseEndpoints.GetPurchaseById(
            createdPurchase.Id.ToString(), "France-Euro", repository, exchangeRateStore, CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<PurchaseEndpoints.PurchaseResponse>>(result.Result);
        Assert.Equal(0.13m, ok.Value!.ConvertedAmount);
    }

    [Fact]
    public async Task GetPurchaseById_matches_currency_case_insensitively()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-03-15T00:00:00Z", 10.00m, Guid.NewGuid());
        var created = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);
        var createdPurchase = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(created.Result).Value!;

        var exchangeRateStore = new ExchangeRateStore();
        exchangeRateStore.SetRates([
            new ExchangeRate(new DateOnly(2026, 3, 1), "France", "Euro", "France-Euro", new DateOnly(2026, 3, 1), 0.9m),
        ]);

        var result = await PurchaseEndpoints.GetPurchaseById(
            createdPurchase.Id.ToString(), "FRANCE-EURO", repository, exchangeRateStore, CancellationToken.None);

        var ok = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<PurchaseEndpoints.PurchaseResponse>>(result.Result);
        Assert.Equal(0.9m, ok.Value!.ExchangeRate);
    }

    [Fact]
    public async Task GetPurchaseById_returns_bad_request_when_currency_is_unknown()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-03-15T00:00:00Z", 10.00m, Guid.NewGuid());
        var created = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);
        var createdPurchase = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(created.Result).Value!;

        var result = await PurchaseEndpoints.GetPurchaseById(
            createdPurchase.Id.ToString(), "Nonexistent-Currency", repository, new ExchangeRateStore(), CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>(result.Result);
    }

    [Fact]
    public async Task GetPurchaseById_returns_bad_request_when_closest_rate_is_older_than_6_months()
    {
        var repository = new FakePurchaseRepository();
        var newPurchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-15T00:00:00Z", 10.00m, Guid.NewGuid());
        var created = await PurchaseEndpoints.PostPurchases(newPurchase, repository, CancellationToken.None);
        var createdPurchase = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Created<Purchase>>(created.Result).Value!;

        var exchangeRateStore = new ExchangeRateStore();
        exchangeRateStore.SetRates([
            // Purchase is 2026-09-15; this rate is from 2026-03-01, more than 6 months earlier.
            new ExchangeRate(new DateOnly(2026, 3, 1), "France", "Euro", "France-Euro", new DateOnly(2026, 3, 1), 0.9m),
        ]);

        var result = await PurchaseEndpoints.GetPurchaseById(
            createdPurchase.Id.ToString(), "France-Euro", repository, exchangeRateStore, CancellationToken.None);

        Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.BadRequest<string>>(result.Result);
    }
}
