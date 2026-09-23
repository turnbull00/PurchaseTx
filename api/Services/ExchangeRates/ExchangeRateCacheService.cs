namespace api.Services.ExchangeRates;

using System.Text.Json;

public class ExchangeRateCacheService(
    TreasuryExchangeRateClient client,
    ExchangeRateStore store,
    IHostEnvironment environment,
    ILogger<ExchangeRateCacheService> logger)
{
    private static readonly DateOnly DefaultStartDate = new(2020, 1, 1);
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private string CacheFilePath => Path.Combine(environment.ContentRootPath, "Data", "exchange-rates.json");

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        var existingRates = await ReadCacheAsync(cancellationToken);

        var sinceDate = existingRates.Count > 0
            ? existingRates.Max(r => r.RecordDate)
            : DefaultStartDate;

        logger.LogInformation(
            "Fetching exchange rates newer than {SinceDate} ({ExistingCount} cached records)",
            sinceDate, existingRates.Count);

        var newRates = await client.GetRatesNewerThanAsync(sinceDate, cancellationToken);

        logger.LogInformation("Fetched {NewCount} new exchange rate records", newRates.Count);

        var mergedRates = existingRates;

        if (newRates.Count > 0)
        {
            mergedRates = [.. existingRates, .. newRates];
            await WriteCacheAsync(mergedRates, cancellationToken);
        }

        store.SetRates(mergedRates);
    }

    private async Task<List<ExchangeRate>> ReadCacheAsync(CancellationToken cancellationToken)
    {
        var path = CacheFilePath;

        if (!File.Exists(path))
        {
            return [];
        }

        await using var stream = File.OpenRead(path);
        var rates = await JsonSerializer.DeserializeAsync<List<ExchangeRate>>(stream, cancellationToken: cancellationToken);

        return rates ?? [];
    }

    private async Task WriteCacheAsync(List<ExchangeRate> rates, CancellationToken cancellationToken)
    {
        var path = CacheFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, rates, SerializerOptions, cancellationToken);
    }
}
