using DotnetArchitecture.Application.Interfaces;
using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;

// Dışarıdan gelecek veriyi tutan Command (Record)
// ICacheInvalidator sayesinde yeni ürün eklendiğinde "products-all" önbelleği otomatik temizlenir.
public record CreateProductCommand(string Name, decimal Price, int InitialStock) : IRequest<Guid>, ICacheInvalidator
{
    public IReadOnlyCollection<string> CacheKeysToInvalidate => new[] { "products-all" };
}