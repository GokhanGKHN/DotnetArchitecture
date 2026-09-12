using MediatR;

namespace DotnetArchitecture.Application.Features.Orders.Queries.GetOrderById;

// Id alıp geriye OrderResponse (veya bulunamazsa null) dönen Query                                                                                                         
public record GetOrderByIdQuery(Guid Id) : IRequest<OrderResponse?>;