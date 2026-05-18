using Kairn.Application.Common;
using Kairn.Application.Features.AR;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Kairn.Infrastructure.Persistence.Services;

public class CustomerService(AppDbContext db) : ICustomerService
{
    public async Task<IReadOnlyList<CustomerDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var customers = await db.Customers
            .Where(c => c.TenantId == tenantId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        var balances = await GetBalancesAsync(tenantId, ct);
        return customers.Select(c => ToDto(c, balances.GetValueOrDefault(c.Id))).ToList();
    }

    public async Task<CustomerDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (customer is null) return null;

        var balances = await GetBalancesAsync(tenantId, ct);
        return ToDto(customer, balances.GetValueOrDefault(id));
    }

    private async Task<Dictionary<Guid, decimal>> GetBalancesAsync(Guid tenantId, CancellationToken ct)
    {
        var activeStatuses = new[] { InvoiceStatus.Sent, InvoiceStatus.PartiallyPaid, InvoiceStatus.Overdue };
        var invoices = await db.Invoices
            .Include(i => i.Lines)
            .Where(i => i.TenantId == tenantId && activeStatuses.Contains(i.Status))
            .ToListAsync(ct);

        return invoices
            .GroupBy(i => i.CustomerId)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(i => i.Lines.Sum(l => l.LineTotal)));
    }

    public async Task<Result<CustomerDto>> CreateAsync(CreateCustomerCommand cmd, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var customer = new Customer
        {
            TenantId = cmd.TenantId,
            Name = cmd.Name,
            Email = cmd.Email,
            Phone = cmd.Phone,
            Address = cmd.Address,
            TaxNumber = cmd.TaxNumber,
            PaymentTermsDays = cmd.PaymentTermsDays,
            CreditLimit = cmd.CreditLimit,
            Currency = cmd.Currency,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Customers.Add(customer);
        await db.SaveChangesAsync(ct);
        return Result<CustomerDto>.Ok(ToDto(customer));
    }

    public async Task<Result<CustomerDto>> UpdateAsync(UpdateCustomerCommand cmd, CancellationToken ct = default)
    {
        var customer = await db.Customers
            .FirstOrDefaultAsync(c => c.Id == cmd.Id && c.TenantId == cmd.TenantId, ct);

        if (customer is null)
            return Result<CustomerDto>.Fail("Customer not found.");

        customer.Name = cmd.Name;
        customer.Email = cmd.Email;
        customer.Phone = cmd.Phone;
        customer.Address = cmd.Address;
        customer.TaxNumber = cmd.TaxNumber;
        customer.PaymentTermsDays = cmd.PaymentTermsDays;
        customer.CreditLimit = cmd.CreditLimit;
        customer.Currency = cmd.Currency;
        customer.IsActive = cmd.IsActive;
        customer.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);
        return Result<CustomerDto>.Ok(ToDto(customer));
    }

    private static CustomerDto ToDto(Customer c, decimal outstandingBalance = 0m) => new(
        c.Id, c.Name, c.Email, c.Phone, c.Address, c.TaxNumber,
        c.PaymentTermsDays, c.CreditLimit, c.Currency, c.IsActive,
        OutstandingBalance: outstandingBalance);
}
