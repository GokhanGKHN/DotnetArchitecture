using DotnetArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Features.Orders.Events;

public class SendEmailOnOrderCreatedHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly ILogger<SendEmailOnOrderCreatedHandler> _logger;

    public SendEmailOnOrderCreatedHandler(ILogger<SendEmailOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Gerçek hayatta burada IEmailService çağrılır                                                                                                                     
        _logger.LogInformation(
            "📧 [E-POSTA BİLDİRİMİ] Müşteriye ({CustomerId}) sipariş onay maili gönderildi. Sipariş: {OrderId}, Tutar: {TotalAmount:C}",
            notification.CustomerId, notification.OrderId, notification.TotalAmount);

        return Task.CompletedTask;
    }
}