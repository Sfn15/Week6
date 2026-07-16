namespace Week6.Dtos.Products;

public record ProductResponse(int ProductId, string Sku, string Name, decimal Price, int StockQuantity);