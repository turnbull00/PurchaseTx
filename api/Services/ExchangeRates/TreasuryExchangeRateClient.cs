namespace api.Services.ExchangeRates;

public class TreasuryExchangeRateClient(HttpClient httpClient)
{
    private const string Fields = "record_date,country,currency,country_currency_desc,effective_date,exchange_rate";
    private const int PageSize = 100;

    public async Task<IReadOnlyList<ExchangeRate>> GetRatesNewerThanAsync(DateOnly since, CancellationToken cancellationToken = default)
    {
        var filter = $"filter=record_date:gt:{since:yyyy-MM-dd}";
        var fields = $"fields={Fields}";
        var query = $"{filter}&{fields}&page[size]={PageSize}&page[number]=1";

        var results = new List<ExchangeRate>();

        while (true)
        {
            var response = await httpClient.GetFromJsonAsync<TreasuryExchangeRatesResponse>($"?{query}", cancellationToken)
                ?? throw new InvalidOperationException("Treasury exchange rate API returned an empty response body.");

            results.AddRange(response.Data);

            var next = response.Links?.Next;
            if (string.IsNullOrEmpty(next))
            {
                break;
            }

            // `next` already begins with '&' and its page[...] params are percent-encoded
            // (e.g. "&page%5Bnumber%5D=2&page%5Bsize%5D=100"). filter/fields are NOT included
            // in it and must be re-supplied on every page request.
            query = $"{filter}&{fields}{Uri.UnescapeDataString(next)}";
        }

        return results;
    }
}
