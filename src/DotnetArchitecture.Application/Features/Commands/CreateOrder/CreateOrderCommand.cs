using MediatR;

namespace DotnetArchitecture.Application.Features.Orders.Commands.CreateOrder;

// Siparişteki her bir kalemi temsil eden istek tipi                                                                                                   
public record OrderItemRequest(Guid ProductId, int Quantity);

// Sipariş oluşturma komutu: Müşteri Id'si ve Kalem Listesi alır, geriye sipariş Id'sini döner                                                         
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemRequest> Items
) : IRequest<Guid>;