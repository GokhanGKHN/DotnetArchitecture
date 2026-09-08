using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using MediatR;

namespace DotnetArchitecture.Application.Features.Orders.Commands.CreateOrder;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, Guid>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        // 1. Yeni bir Sipariş (Aggregate Root) başlatıyoruz                                                                                           
        var order = new Order(request.CustomerId);

        // 2. Her bir sipariş kalemini dönüp Aggregate Root üzerinden siparişe ekliyoruz                                                               
        foreach (var item in request.Items)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId, cancellationToken);
            if (product is null)
                throw new KeyNotFoundException($"Ürün bulunamadı! Id: {item.ProductId}");

            // 💡 DOMAIN İŞ KURALI BURADA ÇALIŞIR:                                                                                                     
            // order.AddItem metodu ürünün stokunu otomatik düşer ve toplam tutarı hesaplar!                                                           
            order.AddItem(product, item.Quantity);
        }

        // 3. Siparişi repository'e ekliyoruz                                                                                                          
        await _orderRepository.AddAsync(order, cancellationToken);

        // 4. Unit of Work: Sipariş, Kalemler ve Güncellenen Ürün Stoku TEK BİR Transaction'da SQL Server'a yazılır!                                   
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.Id;
    }
}