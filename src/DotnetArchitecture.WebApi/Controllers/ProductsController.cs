using DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;
using DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetArchitecture.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly ISender _mediator;

    public ProductsController(ISender mediator)
    {
        _mediator = mediator;
    }

    // POST api/products (Sadece Admin rolündeki kullanıcılar ürün ekleyebilir)
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductCommand command, CancellationToken cancellationToken)
    {
        var productId = await _mediator.Send(command, cancellationToken);
        return Ok(new { Id = productId, Message = "Ürün başarıyla oluşturuldu." });
    }

    // GET api/products?pageNumber=1&pageSize=10&searchTerm=logitech&sortBy=price&isDescending=true
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool isDescending = false,
        CancellationToken cancellationToken = default)
    {
        var query = new GetAllProductsQuery(pageNumber, pageSize, searchTerm, sortBy, isDescending);
        var result = await _mediator.Send(query, cancellationToken);
        return Ok(result);
    }
}
