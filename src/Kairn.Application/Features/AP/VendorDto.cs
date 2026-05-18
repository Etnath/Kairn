namespace Kairn.Application.Features.AP;

public record VendorDto(
    Guid Id,
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    string? IBAN,
    int PaymentTermsDays,
    Guid? DefaultExpenseAccountId,
    string? DefaultExpenseAccountName,
    bool IsActive,
    decimal OutstandingBalance);

public record CreateVendorCommand(
    Guid TenantId,
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    string? IBAN,
    int PaymentTermsDays,
    Guid? DefaultExpenseAccountId);

public record UpdateVendorCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? ContactEmail,
    string? Phone,
    string? Address,
    string? IBAN,
    int PaymentTermsDays,
    Guid? DefaultExpenseAccountId,
    bool IsActive);
