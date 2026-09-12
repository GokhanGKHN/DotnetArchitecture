using MediatR;

namespace DotnetArchitecture.Domain.Common;

// Tüm domain olaylarının türeyeceği temel arayüz                                                                                                                           
public interface IDomainEvent : INotification
{
}