using DotnetArchitecture.Domain.Common;
using DotnetArchitecture.Domain.Enums;

namespace DotnetArchitecture.Domain.Entities;

public class Order : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }

    // Dışarıya sadece okunabilir liste sunuyoruz (Encapsulation)                                                                                      
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public Order(Guid customerId)
    {
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        TotalAmount = 0;
    }

    // İŞ KURALI 1: Siparişe Kalem Ekleme                                                                                                              
    public void AddItem(Product product, int quantity)
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Sadece beklemedeki siparişlere ürün eklenebilir.");

        // Ürünün kendi stok kuralını çalıştırıyoruz                                                                                                   
        product.DeductStock(quantity);

        // Kalemi ekliyoruz                                                                                                                            
        var item = new OrderItem(product.Id, quantity, product.Price);
        _items.Add(item);

        // Toplam tutarı güncelliyoruz                                                                                                                 
        TotalAmount += item.TotalPrice;
    }

    // İŞ KURALI 2: Siparişi İptal Etme                                                                                                                
    public void Cancel()
    {
        if (Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Sipariş zaten iptal edilmiş.");

        Status = OrderStatus.Cancelled;
    }

    // İŞ KURALI 3: Siparişi Tamamlama                                                                                                                 
    public void Complete()
    {
        if (!_items.Any())
            throw new InvalidOperationException("İçinde ürün olmayan sipariş tamamlanamaz.");

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException("Sadece beklemedeki siparişler tamamlanabilir.");

        Status = OrderStatus.Completed;
    }
}