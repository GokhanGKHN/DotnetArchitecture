using DotnetArchitecture.Domain.Common;

namespace DotnetArchitecture.Domain.Entities;

public class Product : BaseEntity
{
    // Dışarıdan sadece okunabilir, kafasına göre kimse değiştiremez!                                                                                  
    public string Name { get; private set; } = string.Empty;
    public decimal Price { get; private set; }
    public int StockQuantity { get; private set; }

    // EF Core ve ORM'ler için boş private constructor gereklidir                                                                                      
    private Product() { }

    // Yeni ürün oluşturulurken geçerli bir durumda doğmasını garanti ediyoruz                                                                         
    public Product(string name, decimal price, int initialStock)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Ürün adı boş olamaz.", nameof(name));

        if (price <= 0)
            throw new ArgumentException("Ürün fiyatı sıfırdan büyük olmalıdır.", nameof(price));

        if (initialStock < 0)
            throw new ArgumentException("Başlangıç stoku negatif olamaz.", nameof(initialStock));

        Name = name;
        Price = price;
        StockQuantity = initialStock;
    }

    // İŞ KURALI 1: Fiyat güncelleme kuralı                                                                                                            
    public void UpdatePrice(decimal newPrice)
    {
        if (newPrice <= 0)
            throw new ArgumentException("Yeni fiyat sıfırdan büyük olmalıdır.", nameof(newPrice));

        Price = newPrice;
    }

    // İŞ KURALI 2: Stok düşme (Sipariş verilince)
    public void DeductStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Düşülecek stok miktarı sıfırdan büyük olmalıdır.", nameof(quantity));

        if (StockQuantity < quantity)
            throw new InvalidOperationException($"Yetersiz stok! Mevcut stok: {StockQuantity}, İstenen: {quantity}");

        StockQuantity -= quantity;
    }

    // İŞ KURALI 3: Stok ekleme (İade veya yeni mal gelince)
    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Eklenecek stok miktarı sıfırdan büyük olmalıdır.", nameof(quantity));

        StockQuantity += quantity;
    }
}
