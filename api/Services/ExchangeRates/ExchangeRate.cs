namespace api.Services.ExchangeRates;

using System.Text.Json.Serialization;

public record ExchangeRate(
    [property: JsonPropertyName("record_date")] DateOnly RecordDate,
    [property: JsonPropertyName("country")] string Country,
    [property: JsonPropertyName("currency")] string Currency,
    [property: JsonPropertyName("country_currency_desc")] string CountryCurrencyDesc,
    [property: JsonPropertyName("effective_date")] DateOnly EffectiveDate,
    [property: JsonPropertyName("exchange_rate")]
    [property: JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    decimal Rate);
