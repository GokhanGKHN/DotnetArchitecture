using DotnetArchitecture.Domain.Entities;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Persistence katmanındaki tüm Fluent API konfigürasyonlarını (tablo kurallarını) otomatik bulur ve uygular                                   
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        base.OnModelCreating(modelBuilder);
    }
}