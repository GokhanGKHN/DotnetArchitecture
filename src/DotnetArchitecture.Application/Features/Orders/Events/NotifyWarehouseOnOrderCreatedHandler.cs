using DotnetArchitecture.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Features.Orders.Events;

public class NotifyWarehouseOnOrderCreatedHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly ILogger<NotifyWarehouseOnOrderCreatedHandler> _logger;

    public NotifyWarehouseOnOrderCreatedHandler(ILogger<NotifyWarehouseOnOrderCreatedHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        // Gerçek hayatta burada Depo / Kargo ERP entegrasyonu çağrılır                                                                                                     
        _logger.LogInformation(
            "📦 [DEPO BİLDİRİMİ] Depo personeline paketleme emri iletildi! Sipariş ID: {OrderId}",
            notification.OrderId);

        return Task.CompletedTask;
    }
}