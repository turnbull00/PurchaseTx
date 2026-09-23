namespace api.Common.Extensions;

using api.Services.ExchangeRates;

public static class ExchangeRateExtensions
{
    private static readonly Uri TreasuryExchangeRatesBaseAddress =
        new("https://api.fiscaldata.treasury.gov/services/api/fiscal_service/v1/accounting/od/rates_of_exchange");

    public static WebApplicationBuilder AddAppExchangeRates(this WebApplicationBuilder builder)
    {
        builder.Services.AddHttpClient<TreasuryExchangeRateClient>(client =>
        {
            client.BaseAddress = TreasuryExchangeRatesBaseAddress;
        });

        builder.Services.AddSingleton<ExchangeRateStore>();
        builder.Services.AddTransient<ExchangeRateCacheService>();

        return builder;
    }
}
