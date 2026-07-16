namespace Week6.Dtos.Customers;

public record CustomerResponse(int CustomerId, string Email, string FirstName, string LastName, DateTime CreatedAt);