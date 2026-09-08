namespace DotnetArchitecture.Application.Features.Products.Queries.GetAllProducts;

// Sadece dışarıya sunmak istediğimiz alanları içeren hafif DTO record'u                                                                               
public record ProductResponse(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity
);