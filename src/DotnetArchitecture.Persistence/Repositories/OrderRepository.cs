using DotnetArchitecture.Application.Common.Specifications;
using DotnetArchitecture.Application.Features.Orders.Specifications;
using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Persistence.Context;
using DotnetArchitecture.Persistence.Specifications;
using Microsoft.EntityFrameworkCore;

namespace DotnetArchitecture.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _context;

    public OrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Aggregate Root mantığı: Siparişi ve kalemlerini Specification ile getiriyoruz
        return await GetBySpecAsync(new OrderWithItemsSpecification(id), cancellationToken);
    }

    public async Task<Order?> GetBySpecAsync(ISpecification<Order> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListAsync(ISpecification<Order> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(spec).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }

    private IQueryable<Order> ApplySpecification(ISpecification<Order> spec)
    {
        return SpecificationEvaluator<Order>.GetQuery(_context.Orders.AsQueryable(), spec);
    }
}