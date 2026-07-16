namespace Week6.Models;

public class Order
{
    public int OrderId { get; set; }
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = default!;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";
    public byte[]? RowVersion { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}