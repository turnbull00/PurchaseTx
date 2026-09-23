namespace api.Tests.Services.ExchangeRates;

using api.Services.ExchangeRates;

public class ExchangeRateStoreTests
{
    private static ExchangeRate Rate(int year, int month, int day, decimal rate, string desc = "France-Euro")
        => new(new DateOnly(year, month, day), "France", "Euro", desc, new DateOnly(year, month, day), rate);

    [Fact]
    public void FindRate_returns_null_when_no_rates_are_loaded()
    {
        var store = new ExchangeRateStore();

        var result = store.FindRate("France-Euro", new DateOnly(2026, 3, 15));

        Assert.Null(result);
    }

    [Fact]
    public void FindRate_excludes_a_rate_dated_after_the_asOf_date()
    {
        var store = new ExchangeRateStore();
        store.SetRates([Rate(2026, 6, 1, 0.9m)]);

        var result = store.FindRate("France-Euro", new DateOnly(2026, 3, 15));

        Assert.Null(result);
    }

    [Fact]
    public void FindRate_picks_the_most_recent_rate_among_multiple_candidates()
    {
        var store = new ExchangeRateStore();
        store.SetRates([
            Rate(2026, 1, 1, 0.80m),
            Rate(2026, 3, 1, 0.85m),
            Rate(2026, 2, 1, 0.82m),
        ]);

        var result = store.FindRate("France-Euro", new DateOnly(2026, 3, 15));

        Assert.NotNull(result);
        Assert.Equal(new DateOnly(2026, 3, 1), result.RecordDate);
        Assert.Equal(0.85m, result.Rate);
    }

    [Fact]
    public void FindRate_includes_a_rate_exactly_6_months_before_the_asOf_date()
    {
        var store = new ExchangeRateStore();
        store.SetRates([Rate(2026, 3, 15, 0.9m)]);

        var result = store.FindRate("France-Euro", new DateOnly(2026, 9, 15));

        Assert.NotNull(result);
        Assert.Equal(0.9m, result.Rate);
    }

    [Fact]
    public void FindRate_excludes_a_rate_older_than_6_months_before_the_asOf_date()
    {
        var store = new ExchangeRateStore();
        store.SetRates([Rate(2026, 3, 14, 0.9m)]);

        var result = store.FindRate("France-Euro", new DateOnly(2026, 9, 15));

        Assert.Null(result);
    }

    [Fact]
    public void FindRate_returns_null_when_currency_does_not_match()
    {
        var store = new ExchangeRateStore();
        store.SetRates([Rate(2026, 3, 1, 0.9m, "Germany-Euro")]);

        var result = store.FindRate("France-Euro", new DateOnly(2026, 3, 15));

        Assert.Null(result);
    }
}
