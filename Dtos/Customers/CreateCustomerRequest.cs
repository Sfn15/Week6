using System.ComponentModel.DataAnnotations;

namespace Week6.Dtos.Customers;

public record CreateCustomerRequest(
    [Required, EmailAddress] string Email,
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName);