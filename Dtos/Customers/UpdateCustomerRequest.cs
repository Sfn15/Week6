using System.ComponentModel.DataAnnotations;

namespace Week6.Dtos.Customers;

public record UpdateCustomerRequest(
    [Required, MaxLength(100)] string FirstName,
    [Required, MaxLength(100)] string LastName,
    [Required, EmailAddress] string Email);