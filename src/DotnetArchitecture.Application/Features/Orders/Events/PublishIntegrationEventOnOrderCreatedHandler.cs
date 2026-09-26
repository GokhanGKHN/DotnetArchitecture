using DotnetArchitecture.Application.Common.Events;
using DotnetArchitecture.Domain.Events;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Features.Orders.Events;

public class PublishIntegrationEventOnOrderCreatedHandler : INotificationHandler<OrderCreatedDomainEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<PublishIntegrationEventOnOrderCreatedHandler> _logger;

    public PublishIntegrationEventOnOrderCreatedHandler(
        IPublishEndpoint publishEndpoint,
        ILogger<PublishIntegrationEventOnOrderCreatedHandler> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Handle(OrderCreatedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("📤 [RABBITMQ YAYINLANIYOR] Sipariş için RabbitMQ'ya mesaj fırlatılıyor... Sipariş ID: {OrderId}", notification.OrderId);

        // MassTransit aracılığıyla RabbitMQ exchange'ine basıyoruz                                                                                     
        await _publishEndpoint.Publish(new OrderCreatedIntegrationEvent(
            notification.OrderId,
            notification.CustomerId,
            notification.TotalAmount,
            DateTime.UtcNow
        ), cancellationToken);

        _logger.LogInformation("✅ [RABBITMQ YAYINLANDI] Mesaj RabbitMQ exchange'ine başarıyla teslim edildi! Sipariş ID: {OrderId}", notification.
OrderId);
    }
}