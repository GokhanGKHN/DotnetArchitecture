using DotnetArchitecture.Application.Common.Specifications;
using DotnetArchitecture.Domain.Entities;

namespace DotnetArchitecture.Application.Features.Orders.Specifications;

/// <summary>
/// Siparişi ve ona ait tüm kalemleri (OrderItem) birlikte getiren Aggregate Root Specification.
/// </summary>
public class OrderWithItemsSpecification : BaseSpecification<Order>
{
    public OrderWithItemsSpecification(Guid orderId)
        : base(o => o.Id == orderId)
    {
        AddInclude(o => o.Items);
    }
}
