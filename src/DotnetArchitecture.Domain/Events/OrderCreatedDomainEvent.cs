using DotnetArchitecture.Domain.Common;

namespace DotnetArchitecture.Domain.Events;

// Olaylar geçmiş zamanda adlandırılır (Created, Cancelled vb.) ve değiştirilemez (record)                                                                                  
public record OrderCreatedDomainEvent(
    Guid OrderId,
    Guid CustomerId,
    decimal TotalAmount
) : IDomainEvent;