using DotnetArchitecture.Application.Features.Orders.Commands.CreateOrder;
using DotnetArchitecture.Application.Features.Orders.Queries.GetOrderById; // 👈 Bu using'i ekle                                                                            
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace DotnetArchitecture.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly ISender _mediator;

    public OrdersController(ISender mediator)
    {
        _mediator = mediator;
    }

    // POST api/orders
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderCommand command, CancellationToken cancellationToken)
    {
        var orderId = await _mediator.Send(command, cancellationToken);
        return Ok(new { Id = orderId, Message = "Sipariş başarıyla oluşturuldu ve stoklar düşüldü." });
    }

    // GET api/orders/{id}
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _mediator.Send(new GetOrderByIdQuery(id), cancellationToken);

        if (order is null)
            return NotFound(new { Message = "Sipariş bulunamadı." });

        return Ok(order);
    }
}
