namespace api.Tests.Validators;

using api.Endpoints;
using api.Validators;

public class NewPurchaseValidatorTests
{
    private readonly NewPurchaseValidator validator = new();

    [Fact]
    public void Valid_purchase_passes()
    {
        var purchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21", 4.50m, Guid.NewGuid());

        var result = validator.Validate(purchase);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Amount_must_be_greater_than_zero(decimal amount)
    {
        var purchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21", amount, Guid.NewGuid());

        var result = validator.Validate(purchase);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PurchaseEndpoints.NewPurchase.Amount));
    }

    [Fact]
    public void Amount_with_more_than_two_decimal_places_is_invalid()
    {
        var purchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21", 4.567m, Guid.NewGuid());

        var result = validator.Validate(purchase);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PurchaseEndpoints.NewPurchase.Amount));
    }

    [Fact]
    public void Description_longer_than_50_characters_is_invalid()
    {
        var purchase = new PurchaseEndpoints.NewPurchase(new string('a', 51), "2026-09-21", 4.50m, Guid.NewGuid());

        var result = validator.Validate(purchase);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PurchaseEndpoints.NewPurchase.Description));
    }

    [Fact]
    public void Unparseable_date_is_invalid()
    {
        var purchase = new PurchaseEndpoints.NewPurchase("Coffee", "not-a-date", 4.50m, Guid.NewGuid());

        var result = validator.Validate(purchase);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PurchaseEndpoints.NewPurchase.Date));
    }

    [Fact]
    public void Empty_ClientPurchaseId_is_invalid()
    {
        var purchase = new PurchaseEndpoints.NewPurchase("Coffee", "2026-09-21", 4.50m, Guid.Empty);

        var result = validator.Validate(purchase);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(PurchaseEndpoints.NewPurchase.ClientPurchaseId));
    }
}
