namespace Week6.Models;

public class Product
{
    public int ProductId { get; set; }
    public string Sku { get; set; } = default!;
    public string Name { get; set; } = default!;
    public decimal Price { get; set; }
    public int StockQuantity { get; set; }
    public byte[]? RowVersion { get; set; }
}