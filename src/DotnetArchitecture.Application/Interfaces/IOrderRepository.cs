using DotnetArchitecture.Application.Common.Specifications;
using DotnetArchitecture.Domain.Entities;

namespace DotnetArchitecture.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Order?> GetBySpecAsync(ISpecification<Order> spec, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Order>> ListAsync(ISpecification<Order> spec, CancellationToken cancellationToken = default);
    Task AddAsync(Order order, CancellationToken cancellationToken = default);
    void Update(Order order);
}
