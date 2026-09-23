namespace api.Services.ExchangeRates;

public class ExchangeRateStore
{
    private IReadOnlyList<ExchangeRate> _rates = [];

    public void SetRates(IReadOnlyList<ExchangeRate> rates) => _rates = rates;

    /// <summary>
    /// Finds the most recent rate for <paramref name="countryCurrencyDesc"/> on or before
    /// <paramref name="asOfDate"/>, as long as it's no more than 6 months older; otherwise null.
    /// </summary>
    public ExchangeRate? FindRate(string countryCurrencyDesc, DateOnly asOfDate)
    {
        var earliestAcceptable = asOfDate.AddMonths(-6);

        return _rates
            .Where(r => string.Equals(r.CountryCurrencyDesc, countryCurrencyDesc, StringComparison.OrdinalIgnoreCase)
                     && r.RecordDate <= asOfDate
                     && r.RecordDate >= earliestAcceptable)
            .OrderByDescending(r => r.RecordDate)
            .FirstOrDefault();
    }
}
