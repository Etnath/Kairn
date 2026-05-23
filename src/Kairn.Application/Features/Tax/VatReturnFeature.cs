using Kairn.Domain.Entities;

namespace Kairn.Application.Features.Tax;

public record VatCategoryRow(
    Guid?        TaxRateId,
    string       TaxRateName,
    TaxCategory? Category,
    decimal      Rate,
    decimal      OutputBase,
    decimal      OutputTax,
    decimal      InputBase,
    decimal      InputTax)
{
    public decimal NetTax => OutputTax - InputTax;
}

public record VatReturnReport(
    DateOnly                     From,
    DateOnly                     To,
    IReadOnlyList<VatCategoryRow> Rows)
{
    public decimal TotalOutputBase => Rows.Sum(r => r.OutputBase);
    public decimal TotalOutputTax  => Rows.Sum(r => r.OutputTax);
    public decimal TotalInputBase  => Rows.Sum(r => r.InputBase);
    public decimal TotalInputTax   => Rows.Sum(r => r.InputTax);
    public decimal NetVatPayable   => TotalOutputTax - TotalInputTax;
}

public record VatDrillDownItem(
    Guid     Id,
    string   Reference,
    DateOnly Date,
    string   CounterpartyName,
    decimal  Base,
    decimal  TaxAmount,
    bool     IsCreditNote);

public interface IVatReturnService
{
    Task<VatReturnReport> GenerateAsync(Guid tenantId, DateOnly from, DateOnly to, CancellationToken ct = default);
    Task<IReadOnlyList<VatDrillDownItem>> GetDrillDownAsync(Guid tenantId, DateOnly from, DateOnly to, Guid? taxRateId, bool isOutput, CancellationToken ct = default);
}

public interface IVatReturnExporter
{
    byte[] ToPdf(VatReturnReport report);
    string ToCsv(VatReturnReport report);
}
