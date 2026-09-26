using DotnetArchitecture.Application.Common.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace DotnetArchitecture.Application.Features.Orders.Consumers;

/// <summary>                                                                                                                                           
/// RabbitMQ üzerindeki kuyruktan OrderCreatedIntegrationEvent mesajlarını dinleyip tüketen (Consume) servis.                                           
/// </summary>                                                                                                                                          
public class OrderCreatedEventConsumer : IConsumer<OrderCreatedIntegrationEvent>
{
    private readonly ILogger<OrderCreatedEventConsumer> _logger;

    public OrderCreatedEventConsumer(ILogger<OrderCreatedEventConsumer> logger)
    {
        _logger = logger;
    }

    public Task Consume(ConsumeContext<OrderCreatedIntegrationEvent> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "🐇 [RABBITMQ CONSUMER] Mesaj kuyruktan başarıyla alındı! Sipariş ID: {OrderId}, Müşteri: {CustomerId}, Tutar: {TotalAmount:C}",
            message.OrderId, message.CustomerId, message.TotalAmount);

        // Gerçek dünyada burada: Fatura kesme, kargo entegrasyonu veya harici bildirim işlemleri yürütülür.                                            

        return Task.CompletedTask;
    }
}