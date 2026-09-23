namespace api.Services.ExchangeRates;

using System.Text.Json.Serialization;

internal record TreasuryExchangeRatesResponse(
    [property: JsonPropertyName("data")] List<ExchangeRate> Data,
    [property: JsonPropertyName("links")] TreasuryExchangeRatesLinks? Links);

internal record TreasuryExchangeRatesLinks(
    [property: JsonPropertyName("next")] string? Next);
