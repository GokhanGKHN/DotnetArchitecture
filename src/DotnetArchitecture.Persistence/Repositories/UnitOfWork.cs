using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Common;
using DotnetArchitecture.Persistence.Context;
using MediatR;

namespace DotnetArchitecture.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private readonly IPublisher _publisher;

    public UnitOfWork(AppDbContext context, IPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Önce veritabanındaki değişiklikleri kaydet                                                                                                                    
        var result = await _context.SaveChangesAsync(cancellationToken);

        // 2. Kayıt başarılı olduysa, değişen entity'lerin üzerindeki tüm Domain Event'leri topla                                                                           
        var domainEvents = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .SelectMany(x =>
            {
                var events = x.DomainEvents.ToList();
                x.ClearDomainEvents(); // Tekrar tetiklenmemesi için temizle                                                                                                
                return events;
            })
            .ToList();

        // 3. Toplanan tüm olayları MediatR üzerinden dinleyicilere (Handlers) dağıt                                                                                        
        foreach (var domainEvent in domainEvents)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        return result;
    }
}