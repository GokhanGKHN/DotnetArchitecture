using System.Text.Json;
using DotnetArchitecture.Persistence.Context;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DotnetArchitecture.WebApi.BackgroundServices;

/// <summary>
/// Veritabanındaki OutboxMessages tablosunu düzenli olarak kontrol eden,
/// henüz işlenmemiş olayları MediatR üzerinden asenkron ve garantili şekilde yayınlayan arka plan servisi.
/// </summary>
public class ProcessOutboxMessagesBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ProcessOutboxMessagesBackgroundService> _logger;

    public ProcessOutboxMessagesBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<ProcessOutboxMessagesBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 [OUTBOX WORKER] Outbox arka plan işleyicisi başlatıldı.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                // Henüz işlenmemiş (ProcessedOnUtc == null) ilk 20 mesajı sıralı olarak al
                var messages = await context.OutboxMessages
                    .Where(m => m.ProcessedOnUtc == null)
                    .OrderBy(m => m.OccurredOnUtc)
                    .Take(20)
                    .ToListAsync(stoppingToken);

                if (messages.Count > 0)
                {
                    _logger.LogInformation("📬 [OUTBOX WORKER] {Count} adet işlenmeyi bekleyen olay bulundu.", messages.Count);

                    foreach (var message in messages)
                    {
                        try
                        {
                            var eventType = Type.GetType(message.Type);
                            if (eventType is null)
                            {
                                _logger.LogWarning("⚠️ [OUTBOX WORKER] Olay tipi çözümlenemedi: {Type}", message.Type);
                                message.Error = "Olay tipi bulunamadı.";
                                message.ProcessedOnUtc = DateTime.UtcNow;
                                continue;
                            }

                            var domainEvent = JsonSerializer.Deserialize(message.Content, eventType);
                            if (domainEvent is INotification notification)
                            {
                                _logger.LogInformation("📢 [OUTBOX YAYINLANIYOR] Olay: {EventType} (ID: {MessageId})", eventType.Name, message.Id);
                                
                                // Olayı dinleyen tüm handler'lara (E-posta, Depo vb.) dağıt
                                await publisher.Publish(notification, stoppingToken);

                                message.ProcessedOnUtc = DateTime.UtcNow;
                                message.Error = null;

                                _logger.LogInformation("✅ [OUTBOX İŞLENDİ] Olay başarıyla işlendi ve tamamlandı: {MessageId}", message.Id);
                            }
                            else
                            {
                                message.Error = "Olay INotification tipine dönüştürülemedi.";
                                message.ProcessedOnUtc = DateTime.UtcNow;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ [OUTBOX HATA] Olay işlenirken hata meydana geldi! ID: {MessageId}", message.Id);
                            message.Error = ex.Message;
                            // Not: Hata durumunda ProcessedOnUtc null kalabilir veya retry mekanizması işletilebilir
                        }
                    }

                    // Güncellenen ProcessedOnUtc / Error durumlarını veritabanına kaydet
                    await context.SaveChangesAsync(stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [OUTBOX WORKER] Arka plan servisinde beklenmeyen bir hata oluştu.");
            }

            // 5 saniye bekle ve sonraki kontrolü yap
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
