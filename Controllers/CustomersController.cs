// Controllers/CustomersController.cs
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Week6.Data;
using Week6.Dtos.Customers;

namespace Week6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerRepository _customers;

    public CustomersController(ICustomerRepository customers) => _customers = customers;

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            var customerId = await _customers.CreateCustomerAsync(request.Email, request.FirstName, request.LastName, ct);
            var response = new CustomerResponse(customerId, request.Email, request.FirstName, request.LastName, DateTime.UtcNow);
            return CreatedAtAction(nameof(GetCustomer), new { id = customerId }, response);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            return Conflict(new { error = ex.Message }); // email already exists
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(int id, UpdateCustomerRequest request, CancellationToken ct)
    {
        try
        {
            await _customers.UpdateCustomerAsync(id, request.FirstName, request.LastName, request.Email, ct);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number is 50002 or 50003)
        {
            // 50002 = not found, 50003 = email taken by another customer
            return ex.Number == 50002 ? NotFound(new { error = ex.Message }) : Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(int id, CancellationToken ct)
    {
        try
        {
            await _customers.SoftDeleteCustomerAsync(id, ct);
            return NoContent();
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<ActionResult<CustomerSearchResponse>> SearchCustomers(
        [FromQuery] string? search, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken ct = default)
    {
        var rows = await _customers.SearchCustomersAsync(search, page, pageSize, ct);
        var customers = rows.Select(r => new CustomerResponse(r.CustomerId, r.Email, r.FirstName, r.LastName, r.CreatedAt)).ToList();
        var totalCount = rows.FirstOrDefault()?.TotalCount ?? 0;

        return Ok(new CustomerSearchResponse(customers, totalCount, page, pageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CustomerResponse>> GetCustomer(int id, CancellationToken ct)
    {
        var customer = await _customers.GetCustomerByIdAsync(id, ct);
        if (customer is null) return NotFound();

        return Ok(new CustomerResponse(customer.CustomerId, customer.Email, customer.FirstName, customer.LastName, customer.CreatedAt));
    }
}