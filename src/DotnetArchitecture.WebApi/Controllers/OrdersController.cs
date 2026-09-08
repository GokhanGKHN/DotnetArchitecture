using DotnetArchitecture.Application.Features.Orders.Commands.CreateOrder;
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
}
