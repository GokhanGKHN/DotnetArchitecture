using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace DotnetArchitecture.Persistence.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Persistence katmanındaki tüm Fluent API konfigürasyonlarını (tablo kurallarını) otomatik bulur ve uygular
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 🗑️ Global Query Filter: ISoftDeletable uygulayan tüm varlıklarda silinmiş (IsDeleted = true) olanları otomatik filtrele
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(DotnetArchitecture.Domain.Common.ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var propertyMethod = typeof(EF).GetMethod(nameof(EF.Property), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public)!
                    .MakeGenericMethod(typeof(bool));
                var isDeletedProperty = System.Linq.Expressions.Expression.Call(propertyMethod, parameter, System.Linq.Expressions.Expression.Constant("IsDeleted"));
                var compareExpression = System.Linq.Expressions.Expression.Equal(isDeletedProperty, System.Linq.Expressions.Expression.Constant(false));
                var lambda = System.Linq.Expressions.Expression.Lambda(compareExpression, parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
            }
        }

        base.OnModelCreating(modelBuilder);
    }
}