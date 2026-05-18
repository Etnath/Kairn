namespace Kairn.Application.Features.AR;

public record CustomerDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? TaxNumber,
    int PaymentTermsDays,
    decimal? CreditLimit,
    string Currency,
    bool IsActive,
    decimal OutstandingBalance);

public record CreateCustomerCommand(
    Guid TenantId,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? TaxNumber,
    int PaymentTermsDays,
    decimal? CreditLimit,
    string Currency);

public record UpdateCustomerCommand(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Email,
    string? Phone,
    string? Address,
    string? TaxNumber,
    int PaymentTermsDays,
    decimal? CreditLimit,
    string Currency,
    bool IsActive);
