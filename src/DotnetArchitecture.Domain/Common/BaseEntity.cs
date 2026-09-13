namespace DotnetArchitecture.Domain.Common;

public abstract class BaseEntity : IAuditableEntity, ISoftDeletable
{
    public Guid Id { get; protected set; } = Guid.NewGuid();

    // 🕒 Denetim İzi (Audit) Alanları
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? LastModifiedAtUtc { get; set; }
    public string? LastModifiedBy { get; set; }

    // 🗑️ Mantıksal Silme (Soft Delete) Alanları
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public string? DeletedBy { get; set; }

    // Entity içinde meydana gelen olayların listesi                                                                                                                        
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}