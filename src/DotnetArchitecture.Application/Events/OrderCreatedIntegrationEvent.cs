namespace DotnetArchitecture.Application.Common.Events;

/// <summary>                                                                                                                                           
/// RabbitMQ mesaj kuyruğuna gönderilecek olan entegrasyon olayı sözleşmesi.                                                                            
/// </summary>                                                                                                                                          
public record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount,
    DateTime CreatedAtUtc
);