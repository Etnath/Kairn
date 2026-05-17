namespace Kairn.Application.Features.Reports;

public interface ITrialBalanceExporter
{
    byte[] ToPdf(TrialBalanceReport report);
    string ToCsv(TrialBalanceReport report);
}
