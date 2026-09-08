using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using DotnetArchitecture.Persistence.Context;
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
        // Aggregate Root mantığı: Siparişi çekerken kalemlerini de (Items) beraberinde getiriyoruz                                                    
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
    }

    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
    {
        await _context.Orders.AddAsync(order, cancellationToken);
    }

    public void Update(Order order)
    {
        _context.Orders.Update(order);
    }
}