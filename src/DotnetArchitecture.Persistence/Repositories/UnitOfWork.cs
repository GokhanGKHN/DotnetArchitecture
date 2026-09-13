using System.Text.Json;
using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Common;
using DotnetArchitecture.Persistence.Context;
using DotnetArchitecture.Persistence.Outbox;

namespace DotnetArchitecture.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // 1. Değişen entity'lerin üzerindeki tüm Domain Event'leri topla
        var domainEvents = _context.ChangeTracker
            .Entries<BaseEntity>()
            .Select(x => x.Entity)
            .SelectMany(x =>
            {
                var events = x.DomainEvents.ToList();
                x.ClearDomainEvents(); // Tekrar tetiklenmemesi için entity üzerinden temizle
                return events;
            })
            .ToList();

        // 2. Her bir olayı OutboxMessage olarak hazırla
        var outboxMessages = domainEvents.Select(domainEvent => new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredOnUtc = DateTime.UtcNow,
            Type = domainEvent.GetType().AssemblyQualifiedName ?? domainEvent.GetType().FullName ?? domainEvent.GetType().Name,
            Content = JsonSerializer.Serialize(domainEvent, domainEvent.GetType())
        }).ToList();

        if (outboxMessages.Count > 0)
        {
            await _context.OutboxMessages.AddRangeAsync(outboxMessages, cancellationToken);
        }

        // 3. Sipariş, kalemler, stok güncellemesi ve Outbox mesajlarını TEK BİR ATOMİK TRANSACTION ile kaydet!
        return await _context.SaveChangesAsync(cancellationToken);
    }
}