using Week6.Models;

namespace Week6.Dtos.Customers;

public static class CustomerMappings
{
    public static CustomerResponse ToResponse(this Customer customer) =>
        new(customer.CustomerId, customer.Email, customer.FirstName, customer.LastName, customer.CreatedAt);
}