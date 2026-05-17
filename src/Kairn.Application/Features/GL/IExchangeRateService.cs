namespace Kairn.Application.Features.GL;

public record RateResult(decimal Rate, DateOnly RateDate, bool IsFromCache);

public interface IExchangeRateService
{
    string BaseCurrency { get; }
    IReadOnlyList<string> SupportedCurrencies { get; }
    Task<RateResult> GetRateAsync(string currency, DateOnly date, CancellationToken ct = default);
    Task RefreshRatesAsync(CancellationToken ct = default);
}
