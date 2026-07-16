using Week6.Models;

namespace Week6.Dtos.Products;

public static class ProductMappings
{
    public static ProductResponse ToResponse(this Product product) =>
        new(product.ProductId, product.Sku, product.Name, product.Price, product.StockQuantity);
}