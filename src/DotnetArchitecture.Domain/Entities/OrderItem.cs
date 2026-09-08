using DotnetArchitecture.Domain.Common;

namespace DotnetArchitecture.Domain.Entities;

public class OrderItem : BaseEntity
{
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }

    // Toplam tutar: Adet * Birim Fiyat                                                                                                                
    public decimal TotalPrice => Quantity * UnitPrice;

    private OrderItem() { }

    // Constructor 'internal' çünkü OrderItem yalnızca Order (Aggregate Root) tarafından yaratılmalıdır!                                               
    internal OrderItem(Guid productId, int quantity, decimal unitPrice)
    {
        if (quantity <= 0)
            throw new ArgumentException("Miktar sıfırdan büyük olmalıdır.", nameof(quantity));

        if (unitPrice <= 0)
            throw new ArgumentException("Birim fiyat sıfırdan büyük olmalıdır.", nameof(unitPrice));

        ProductId = productId;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}