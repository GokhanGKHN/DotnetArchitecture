using DotnetArchitecture.Application.Interfaces;
using MediatR;

namespace DotnetArchitecture.Application.Features.Orders.Queries.GetOrderById;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderResponse?>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<OrderResponse?> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        // Repository'miz Aggregate Root gereği kalemleri (Items) otomatik Include ederek çeker                                                                             
        var order = await _orderRepository.GetByIdAsync(request.Id, cancellationToken);

        if (order is null)
            return null;

        // Entity'yi DTO'ya dönüştürüyoruz (Mapping)                                                                                                                        
        var itemResponses = order.Items.Select(item => new OrderItemResponse(
            item.ProductId,
            item.Quantity,
            item.UnitPrice,
            item.TotalPrice
        )).ToList();

        return new OrderResponse(
            order.Id,
            order.CustomerId,
            order.Status.ToString(), // Enum'ı metin olarak dönüyoruz (Örn: "Pending")                                                                                      
            order.TotalAmount,
            order.CreatedAtUtc,
            itemResponses
        );
    }
}