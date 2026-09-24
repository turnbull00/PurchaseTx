namespace api.Endpoints;
using System.Globalization;
using System.Text.Json.Serialization;

using api.Models;
using api.Services.ExchangeRates;

using FluentValidation;
using Microsoft.AspNetCore.Http.HttpResults;



public static class PurchaseEndpoints
{
    public static void MapPurchaseEndpoints(this IEndpointRouteBuilder app)
    {
        var purchases = app.MapGroup("/purchase");
        // purchases.MapGet("/", GetPurchases);
        purchases.MapGet("/{id}", GetPurchaseById);

        purchases.MapPost("/", PostPurchases).AddEndpointFilter(ValidatePurchase);
    }

    private static async ValueTask<object?> ValidatePurchase(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var purchaseInput = context.GetArgument<NewPurchase>(0);
        var validator = context.HttpContext.RequestServices.GetRequiredService<IValidator<NewPurchase>>();
        var validationResult = await validator.ValidateAsync(purchaseInput);

        if (!validationResult.IsValid)
        {
            return Results.ValidationProblem(validationResult.ToDictionary());
        }

        return await next(context);
    }

    internal static async Task<Ok<IReadOnlyList<Purchase>>> GetPurchases(IPurchaseRepository repository, CancellationToken cancellationToken)
    {
        return TypedResults.Ok(await repository.GetAllAsync(cancellationToken));
    }

    internal record PurchaseResponse(
        long Id,
        string Description,
        DateTime Date,
        decimal Amount,
        Guid ClientPurchaseId,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] string? Currency = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ExchangeRate = null,
        [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] decimal? ConvertedAmount = null);

    internal static async Task<Results<Ok<PurchaseResponse>, NotFound, BadRequest<string>>> GetPurchaseById(
        string id, string? currency, IPurchaseRepository repository, ExchangeRateStore exchangeRateStore, CancellationToken cancellationToken)
    {
        Purchase? purchase = long.TryParse(id, out var purchaseId)
            ? await repository.GetByIdAsync(purchaseId, cancellationToken)
            : Guid.TryParse(id, out var clientPurchaseId)
                ? await repository.GetByClientPurchaseIdAsync(clientPurchaseId, cancellationToken)
                : null;

        if (purchase is null)
        {
            return TypedResults.NotFound();
        }

        if (string.IsNullOrEmpty(currency))
        {
            return TypedResults.Ok(new PurchaseResponse(
                purchase.Id, purchase.Description, purchase.Date, purchase.Amount, purchase.ClientPurchaseId));
        }

        var purchaseDate = DateOnly.FromDateTime(purchase.Date);
        var rate = exchangeRateStore.FindRate(currency, purchaseDate);

        if (rate is null)
        {
            return TypedResults.BadRequest(
                $"No exchange rate available for '{currency}' within 6 months before {purchaseDate:yyyy-MM-dd}.");
        }

        var convertedAmount = Math.Round(purchase.Amount * rate.Rate, 2);

        return TypedResults.Ok(new PurchaseResponse(
            purchase.Id, purchase.Description, purchase.Date, purchase.Amount, purchase.ClientPurchaseId,
            currency, rate.Rate, convertedAmount));
    }

    internal record NewPurchase(string Description, string Date, decimal Amount, Guid ClientPurchaseId);

    internal static async Task<Results<Created<Purchase>, Conflict<string>>> PostPurchases(NewPurchase newPurchase, IPurchaseRepository repository, CancellationToken cancellationToken)
    {
        var date = DateTime.Parse(newPurchase.Date, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal);
        Purchase item = new Purchase(default, newPurchase.Description, date, newPurchase.Amount + 0.00m, newPurchase.ClientPurchaseId);

        try
        {
            item = await repository.AddAsync(item, cancellationToken);
        }
        catch (DuplicateClientPurchaseIdException ex)
        {
            return TypedResults.Conflict(ex.Message);
        }

        return TypedResults.Created($"/purchase/{item.Id}", item);
    }
}
