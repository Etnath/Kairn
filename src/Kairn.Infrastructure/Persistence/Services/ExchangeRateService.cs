using Kairn.Application.Features.GL;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace Kairn.Infrastructure.Persistence.Services;

public class ExchangeRateService(
    AppDbContext db,
    IHttpClientFactory httpFactory,
    IConfiguration config,
    ILogger<ExchangeRateService> logger) : IExchangeRateService
{
    public string BaseCurrency =>
        config["Currency:Base"] ?? "EUR";

    public IReadOnlyList<string> SupportedCurrencies { get; } =
        new[] { "EUR", "USD", "GBP", "CHF" };

    public async Task<RateResult> GetRateAsync(string currency, DateOnly date, CancellationToken ct = default)
    {
        if (string.Equals(currency, BaseCurrency, StringComparison.OrdinalIgnoreCase))
            return new RateResult(1m, date, false);

        var pair = $"{currency}/{BaseCurrency}";

        // Exact cache hit
        var cached = await db.CurrencyRates
            .FirstOrDefaultAsync(r => r.CurrencyPair == pair && r.Date == date, ct);

        if (cached is not null)
            return new RateResult(cached.Rate, cached.Date, false);

        // Try Frankfurter API
        try
        {
            var fetched = await FetchFromApiAsync(currency, date, ct);
            if (fetched is not null)
            {
                // Upsert — API may return a different date (weekend → prev business day)
                var existing = await db.CurrencyRates
                    .FirstOrDefaultAsync(r => r.CurrencyPair == pair && r.Date == fetched.Value.Date, ct);

                if (existing is null)
                {
                    db.CurrencyRates.Add(new CurrencyRate
                    {
                        CurrencyPair = pair,
                        Date = fetched.Value.Date,
                        Rate = fetched.Value.Rate,
                        FetchedAt = DateTimeOffset.UtcNow,
                    });
                    await db.SaveChangesAsync(ct);
                }

                return new RateResult(fetched.Value.Rate, fetched.Value.Date, false);
            }
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Frankfurter API unavailable for {Pair} on {Date}", pair, date);
        }

        // Fall back to most recent cached rate
        var fallback = await db.CurrencyRates
            .Where(r => r.CurrencyPair == pair)
            .OrderByDescending(r => r.Date)
            .FirstOrDefaultAsync(ct);

        if (fallback is not null)
        {
            logger.LogWarning("Using cached exchange rate for {Pair} from {Date}", pair, fallback.Date);
            return new RateResult(fallback.Rate, fallback.Date, true);
        }

        logger.LogError("No exchange rate available for {Pair} on {Date}; defaulting to 1.0", pair, date);
        return new RateResult(1m, date, true);
    }

    public async Task RefreshRatesAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        foreach (var currency in SupportedCurrencies
            .Where(c => !string.Equals(c, BaseCurrency, StringComparison.OrdinalIgnoreCase)))
        {
            await GetRateAsync(currency, today, ct);
        }
    }

    private async Task<(decimal Rate, DateOnly Date)?> FetchFromApiAsync(
        string foreignCurrency, DateOnly date, CancellationToken ct)
    {
        var baseUrl = config["FrankfurterApi:BaseUrl"] ?? "https://api.frankfurter.dev";
        var dateStr = date >= DateOnly.FromDateTime(DateTime.UtcNow.Date)
            ? "latest"
            : date.ToString("yyyy-MM-dd");

        var url = $"{baseUrl}/{dateStr}?base={foreignCurrency}&symbols={BaseCurrency}";
        var http = httpFactory.CreateClient("Frankfurter");

        var response = await http.GetAsync(url, ct);
        response.EnsureSuccessStatusCode();

        var data = await response.Content.ReadFromJsonAsync<FrankfurterResponse>(ct);
        if (data?.Rates is null || !data.Rates.TryGetValue(BaseCurrency, out var rate))
            return null;

        var rateDate = DateOnly.TryParse(data.Date, out var d) ? d : date;
        return (rate, rateDate);
    }

    private sealed class FrankfurterResponse
    {
        [JsonPropertyName("date")] public string? Date { get; set; }
        [JsonPropertyName("rates")] public Dictionary<string, decimal>? Rates { get; set; }
    }
}
