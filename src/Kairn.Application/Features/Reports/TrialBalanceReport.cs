using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Reports;

public record TrialBalanceRow(
    string AccountCode,
    string AccountName,
    AccountType AccountType,
    decimal DebitBalance,
    decimal CreditBalance);

public record TrialBalanceReport(DateOnly AsOf, IReadOnlyList<TrialBalanceRow> Rows)
{
    public decimal TotalDebit  => Rows.Sum(r => r.DebitBalance);
    public decimal TotalCredit => Rows.Sum(r => r.CreditBalance);
    public bool IsBalanced     => Math.Abs(TotalDebit - TotalCredit) < 0.01m;
}
