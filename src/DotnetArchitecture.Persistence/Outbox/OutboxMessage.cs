namespace DotnetArchitecture.Persistence.Outbox;

/// <summary>
/// Domain Event'lerin aynı veritabanı transaction'ında güvenle saklandığı Outbox mesaj entity'si.
/// </summary>
public class OutboxMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Olayın CLR tipi (Reflection ile deserialize etmek için tam tip adı).
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Olayın JSON verisi (Payload).
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Olayın gerçekleştiği UTC zamanı.
    /// </summary>
    public DateTime OccurredOnUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Olayın arka plan servisi tarafından başarıyla işlendiği zaman (Henüz işlenmediyse null).
    /// </summary>
    public DateTime? ProcessedOnUtc { get; set; }

    /// <summary>
    /// İşleme sırasında bir hata oluştuysa hata mesajı.
    /// </summary>
    public string? Error { get; set; }
}
