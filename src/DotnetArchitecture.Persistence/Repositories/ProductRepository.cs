using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace DotnetArchitecture.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        // AsNoTracking: Sadece okuma yaptığımız için EF Core'un nesneleri bellekte takip etme maliyetini sıfırlar (yüksek performans)                 
        return await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        await _context.Products.AddAsync(product, cancellationToken);
    }

    public void Update(Product product)
    {
        _context.Products.Update(product);
    }
}