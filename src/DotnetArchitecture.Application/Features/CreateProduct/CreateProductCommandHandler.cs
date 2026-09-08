using DotnetArchitecture.Application.Interfaces;
using DotnetArchitecture.Domain.Entities;
using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(IProductRepository productRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        // 1. Domain nesnesini oluşturuyoruz (Domain kuralları burada devreye girer!)
        var product = new Product(request.Name, request.Price, request.InitialStock);

        // 2. Repository aracılığıyla ekleme kuyruğuna alıyoruz
        await _productRepository.AddAsync(product, cancellationToken);

        // 3. Değişiklikleri veritabanına tek seferde yansıtıyoruz (Unit of Work)
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 4. Yeni ürünün Id'sini dönüyoruz
        return product.Id;
    }
}
