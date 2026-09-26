using DotnetArchitecture.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DotnetArchitecture.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentGateway _paymentGateway;

    public PaymentsController(IPaymentGateway paymentGateway)
    {
        _paymentGateway = paymentGateway;
    }

    /// <summary>
    /// Harici ödeme servisi üzerinden ödeme işlemini başlatır.
    /// Farklı hata toleransı senaryoları simülasyon modu (Normal, Flaky, Outage, Slow) ile test edilebilir.
    /// </summary>
    [HttpPost("process")]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request, CancellationToken cancellationToken)
    {
        var result = await _paymentGateway.ProcessPaymentAsync(request, cancellationToken);

        if (!result.IsSuccess)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                success = false,
                message = result.ErrorMessage,
                orderId = request.OrderId
            });
        }

        return Ok(new
        {
            success = true,
            transactionId = result.TransactionId,
            orderId = request.OrderId,
            amount = request.Amount,
            currency = request.Currency
        });
    }
}
