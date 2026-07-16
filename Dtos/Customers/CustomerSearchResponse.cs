namespace Week6.Dtos.Customers;

public record CustomerSearchResponse(List<CustomerResponse> Customers, int TotalCount, int PageNumber, int PageSize);