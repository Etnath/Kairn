using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Kairn.Infrastructure.Persistence.Configurations;

public class DepreciationLogConfiguration : IEntityTypeConfiguration<DepreciationLog>
{
    public void Configure(EntityTypeBuilder<DepreciationLog> b)
    {
        b.HasKey(x => x.Id);
        b.Property(x => x.Id).ValueGeneratedOnAdd();
        b.Property(x => x.AssetName).IsRequired().HasMaxLength(200);
        b.Property(x => x.ErrorMessage).HasMaxLength(2000);
        b.Property(x => x.PostedReference).HasMaxLength(30);
        b.HasIndex(x => new { x.TenantId, x.Period, x.IsSuccess });
    }
}
