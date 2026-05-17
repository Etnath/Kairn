using System.Text.Json;
using Kairn.Application.Common;
using Kairn.Domain.Common;
using Kairn.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Kairn.Infrastructure.Persistence.Interceptors;

public class AuditLogInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserContext _currentUser;

    public AuditLogInterceptor(ICurrentUserContext currentUser) => _currentUser = currentUser;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
            AuditChanges(eventData.Context);

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AuditChanges(DbContext context)
    {
        var now = DateTimeOffset.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in context.ChangeTracker.Entries<BaseEntity>().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = now;
                    entry.Entity.UpdatedAt = now;
                    WriteAuditLog(context, entry.Entity.GetType().Name,
                        entry.Entity.Id.ToString(), "Created", userId,
                        null, Serialize(entry.CurrentValues.ToObject()));
                    break;

                case EntityState.Modified:
                    entry.Entity.UpdatedAt = now;
                    var action = DetermineModifiedAction(entry);
                    WriteAuditLog(context, entry.Entity.GetType().Name,
                        entry.Entity.Id.ToString(), action, userId,
                        Serialize(entry.OriginalValues.ToObject()),
                        Serialize(entry.CurrentValues.ToObject()));
                    break;

                case EntityState.Deleted:
                    WriteAuditLog(context, entry.Entity.GetType().Name,
                        entry.Entity.Id.ToString(), "Deleted", userId,
                        Serialize(entry.OriginalValues.ToObject()), null);
                    break;
            }
        }
    }

    private static string DetermineModifiedAction(Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<BaseEntity> entry)
    {
        var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "IsDeleted");
        if (prop is not null)
        {
            var wasDeleted = prop.OriginalValue is true;
            var isDeleted  = prop.CurrentValue  is true;
            if (!wasDeleted && isDeleted) return "Deleted";
            if (wasDeleted && !isDeleted) return "Restored";
        }
        return "Updated";
    }

    private static void WriteAuditLog(DbContext context, string entityType, string recordId,
        string action, string changedBy, string? oldValues, string? newValues)
    {
        context.Set<AuditLog>().Add(new AuditLog
        {
            EntityType = entityType,
            RecordId = recordId,
            Action = action,
            ChangedBy = changedBy,
            ChangedAt = DateTimeOffset.UtcNow,
            OldValues = oldValues,
            NewValues = newValues,
        });
    }

    private static string? Serialize(object? obj) =>
        obj is null ? null : JsonSerializer.Serialize(obj);
}
