using DotnetArchitecture.Application.Common.Specifications;
using DotnetArchitecture.Application.Features.Products.Specifications;
using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Persistence.Context;
using DotnetArchitecture.Persistence.Specifications;
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

    public async Task<Product?> GetBySpecAsync(ISpecification<Product> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Products.AsNoTracking().ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(ISpecification<Product> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }

    public async Task<int> CountAsync(ISpecification<Product> spec, CancellationToken cancellationToken = default)
    {
        var query = _context.Products.AsNoTracking().AsQueryable();
        if (spec.Criteria != null)
        {
            query = query.Where(spec.Criteria);
        }
        return await query.CountAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        int pageNumber,
        int pageSize,
        string? searchTerm = null,
        string? sortBy = null,
        bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        var filterSpec = new ProductsFilterSpecification(pageNumber, pageSize, searchTerm, sortBy, isDescending);
        var countSpec = new ProductsCountSpecification(searchTerm);

        var totalCount = await CountAsync(countSpec, cancellationToken);
        var items = await ListAsync(filterSpec, cancellationToken);

        return (items, totalCount);
    }

    private IQueryable<Product> ApplySpecification(ISpecification<Product> spec)
    {
        return SpecificationEvaluator<Product>.GetQuery(_context.Products.AsNoTracking().AsQueryable(), spec);
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