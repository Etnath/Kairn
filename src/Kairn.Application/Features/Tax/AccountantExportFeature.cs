namespace Kairn.Application.Features.Tax;

public interface IAccountantExportService
{
    Task<byte[]> GenerateZipAsync(
        Guid tenantId, DateOnly from, DateOnly to, string generatedBy,
        CancellationToken ct = default);
}
