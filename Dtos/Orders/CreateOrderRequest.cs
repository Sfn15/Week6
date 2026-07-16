using System.ComponentModel.DataAnnotations;

namespace Week6.Dtos.Orders;

public record CreateOrderRequest(
    [Required]int CustomerId, 
    [Required]int ProductId, 
    [Required]int Quantity);