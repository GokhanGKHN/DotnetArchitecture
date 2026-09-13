using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace DotnetArchitecture.Persistence.Interceptors;

/// <summary>
/// EF Core ChangeTracker üzerinden varlıkların oluşturulma, güncellenme ve mantıksal silinme (Soft Delete)
/// işlemlerini SaveChanges aşamasında otomatik olarak yöneten Interceptor.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService? _currentUserService;

    public AuditableEntityInterceptor(ICurrentUserService? currentUserService = null)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        var currentUser = _currentUserService?.UserEmail
            ?? _currentUserService?.UserId
            ?? "system";

        var utcNow = DateTime.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            // 1. Audit Bilgilerini Güncelle
            if (entry.Entity is IAuditableEntity auditableEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    auditableEntity.CreatedAtUtc = utcNow;
                    auditableEntity.CreatedBy ??= currentUser;
                }
                else if (entry.State == EntityState.Modified)
                {
                    auditableEntity.LastModifiedAtUtc = utcNow;
                    auditableEntity.LastModifiedBy = currentUser;
                }
            }

            // 2. Soft Delete (Fiziksel Silmeyi Mantıksal Silmeye Dönüştür)
            if (entry.Entity is ISoftDeletable softDeletable && entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                softDeletable.IsDeleted = true;
                softDeletable.DeletedAtUtc = utcNow;
                softDeletable.DeletedBy = currentUser;
            }
        }
    }
}
