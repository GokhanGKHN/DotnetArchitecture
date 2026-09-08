using MediatR;

namespace DotnetArchitecture.Application.Features.Products.Commands.CreateProduct;

// Dışarıdan gelecek veriyi tutan Command (Record)                                                                                                     
// IRequest<Guid> : Bu işlem tamamlandığında geriye oluşturulan ürünün Id'sini (Guid) dönecek demektir.                                                
public record CreateProductCommand(string Name, decimal Price, int InitialStock) : IRequest<Guid>;