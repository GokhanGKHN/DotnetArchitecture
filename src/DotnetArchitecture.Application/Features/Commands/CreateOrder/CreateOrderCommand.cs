using DotnetArchitecture.Application.Common.Idempotency;
using MediatR;

namespace DotnetArchitecture.Application.Features.Orders.Commands.CreateOrder;

// Siparişteki her bir kalemi temsil eden istek tipi
public record OrderItemRequest(Guid ProductId, int Quantity);

// Sipariş oluşturma komutu: Müşteri Id'si, Kalem Listesi ve opsiyonel Idempotency-Key alır.
// IIdempotentCommand sayesinde mükerrer isteklerde (çift tıklama/yeniden deneme) sipariş iki kez oluşturulmaz ve stoklar çift düşülmez!
public record CreateOrderCommand(
    Guid CustomerId,
    List<OrderItemRequest> Items,
    Guid? IdempotencyKey = null
) : IIdempotentCommand<Guid>;