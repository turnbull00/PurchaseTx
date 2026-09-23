namespace api.Validators;

using api.Endpoints;

using FluentValidation;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPurchaseValidators(this IServiceCollection services)
    {
        services.AddScoped<IValidator<PurchaseEndpoints.NewPurchase>, NewPurchaseValidator>();

        return services;
    }
}
