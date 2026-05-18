using Kairn.Application.Common;
using Kairn.Application.Features.AP;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using static Kairn.Domain.Entities.BillStatus;

namespace Kairn.Infrastructure.Persistence.Services;

public class VendorService(AppDbContext db) : IVendorService
{
    public async Task<IReadOnlyList<VendorDto>> GetAllAsync(Guid tenantId, CancellationToken ct = default)
    {
        var vendors = await db.Vendors
            .Include(v => v.DefaultExpenseAccount)
            .Where(v => v.TenantId == tenantId)
            .OrderBy(v => v.Name)
            .ToListAsync(ct);

        var outstanding = await db.Bills
            .Where(b => b.TenantId == tenantId
                     && (b.Status == Approved || b.Status == PartiallyPaid || b.Status == Overdue))
            .GroupBy(b => b.VendorId)
            .Select(g => new { VendorId = g.Key, Total = g.Sum(b => b.GrandTotal - b.AmountPaid) })
            .ToListAsync(ct);
        var outstandingMap = outstanding.ToDictionary(x => x.VendorId, x => x.Total);

        return vendors.Select(v => ToDto(v, outstandingMap.GetValueOrDefault(v.Id, 0m))).ToList();
    }

    public async Task<VendorDto?> GetByIdAsync(Guid id, Guid tenantId, CancellationToken ct = default)
    {
        var vendor = await db.Vendors
            .Include(v => v.DefaultExpenseAccount)
            .FirstOrDefaultAsync(v => v.Id == id && v.TenantId == tenantId, ct);

        if (vendor is null) return null;

        var outstanding = await db.Bills
            .Where(b => b.TenantId == tenantId && b.VendorId == id
                     && (b.Status == Approved || b.Status == PartiallyPaid || b.Status == Overdue))
            .SumAsync(b => b.GrandTotal - b.AmountPaid, ct);

        return ToDto(vendor, outstanding);
    }

    public async Task<Result<VendorDto>> CreateAsync(CreateVendorCommand cmd, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var vendor = new Vendor
        {
            TenantId = cmd.TenantId,
            Name = cmd.Name,
            ContactEmail = cmd.ContactEmail,
            Phone = cmd.Phone,
            Address = cmd.Address,
            IBAN = cmd.IBAN,
            PaymentTermsDays = cmd.PaymentTermsDays,
            DefaultExpenseAccountId = cmd.DefaultExpenseAccountId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };

        db.Vendors.Add(vendor);
        await db.SaveChangesAsync(ct);

        var saved = await db.Vendors
            .Include(v => v.DefaultExpenseAccount)
            .FirstAsync(v => v.Id == vendor.Id, ct);

        var savedOutstanding = await db.Bills
            .Where(b => b.TenantId == cmd.TenantId && b.VendorId == saved.Id
                     && (b.Status == Approved || b.Status == PartiallyPaid || b.Status == Overdue))
            .SumAsync(b => b.GrandTotal - b.AmountPaid, ct);
        return Result<VendorDto>.Ok(ToDto(saved, savedOutstanding));
    }

    public async Task<Result<VendorDto>> UpdateAsync(UpdateVendorCommand cmd, CancellationToken ct = default)
    {
        var vendor = await db.Vendors
            .FirstOrDefaultAsync(v => v.Id == cmd.Id && v.TenantId == cmd.TenantId, ct);

        if (vendor is null)
            return Result<VendorDto>.Fail("Vendor not found.");

        vendor.Name = cmd.Name;
        vendor.ContactEmail = cmd.ContactEmail;
        vendor.Phone = cmd.Phone;
        vendor.Address = cmd.Address;
        vendor.IBAN = cmd.IBAN;
        vendor.PaymentTermsDays = cmd.PaymentTermsDays;
        vendor.DefaultExpenseAccountId = cmd.DefaultExpenseAccountId;
        vendor.IsActive = cmd.IsActive;
        vendor.UpdatedAt = DateTimeOffset.UtcNow;

        await db.SaveChangesAsync(ct);

        var saved = await db.Vendors
            .Include(v => v.DefaultExpenseAccount)
            .FirstAsync(v => v.Id == vendor.Id, ct);

        var updatedOutstanding = await db.Bills
            .Where(b => b.TenantId == cmd.TenantId && b.VendorId == saved.Id
                     && (b.Status == Approved || b.Status == PartiallyPaid || b.Status == Overdue))
            .SumAsync(b => b.GrandTotal - b.AmountPaid, ct);
        return Result<VendorDto>.Ok(ToDto(saved, updatedOutstanding));
    }

    private static VendorDto ToDto(Vendor v, decimal outstandingBalance) => new(
        v.Id,
        v.Name,
        v.ContactEmail,
        v.Phone,
        v.Address,
        v.IBAN,
        v.PaymentTermsDays,
        v.DefaultExpenseAccountId,
        v.DefaultExpenseAccount?.Name,
        v.IsActive,
        OutstandingBalance: outstandingBalance);
}
