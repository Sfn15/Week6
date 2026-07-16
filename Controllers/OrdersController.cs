
using Week6.Services;
using Microsoft.AspNetCore.Mvc;
using Week6.Data;
using Week6.Dtos.Orders;
using Week6.Dtos.Customers;

namespace Week6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IOrderProcessingQueue _queue;
    private readonly IOrderRepository _orders;

    public OrdersController(IOrderProcessingQueue queue, IOrderRepository orders)
    {
        _queue = queue;
        _orders = orders;

    }

    // Fake order creation for now — real version comes once Exercise 4 gives us
    // sp_CreateCustomerOrder to call via Dapper.
    [HttpPost]
    public async Task<IActionResult> CreateOrder(CreateOrderRequest request, CancellationToken ct)
    {
        try
        {
            var orderId = await _orders.CreateOrderAsync(request.CustomerId, request.ProductId, request.Quantity, ct);
            await _queue.EnqueueAsync(orderId, ct);
            return Accepted(new OrderResponse(orderId, "Queued"));
        } catch (Microsoft.Data.SqlClient.SqlException e) when (e.Number is 50001 or 50004)
        {
            return BadRequest(new {error = e.Message});
        }
    }
}