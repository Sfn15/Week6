using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
//using Week6.Models;

namespace Week6.Data;

public interface IOrderRepository
{
    Task<int> CreateOrderAsync(int customerId, int productId, int quantity, CancellationToken ct = default);
    Task ProcessPaymentAsync(int orderId, bool success, CancellationToken ct = default);
    Task CancelOrderAsync(int orderId, CancellationToken ct = default);
    Task<List<OrderReportRow>> GetOrderReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default);
}

public record OrderReportRow(int OrderId, DateTime OrderDate, string Status, string CustomerEmail, decimal OrderTotal);

public class OrderRepository : IOrderRepository
{
    private readonly Week6DbContext _db;

    public OrderRepository(Week6DbContext db) => _db = db;

    public async Task<int> CreateOrderAsync(int customerId, int productId, int quantity, CancellationToken ct = default)
    {
        var connection = (SqlConnection)_db.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync(ct);

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_CreateCustomerOrder";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@CustomerId", customerId));
            cmd.Parameters.Add(new SqlParameter("@ProductId", productId));
            cmd.Parameters.Add(new SqlParameter("@Quantity", quantity));
            var outputParam = new SqlParameter("@OrderId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outputParam);

            await cmd.ExecuteNonQueryAsync(ct);
            return (int)outputParam.Value;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    public Task ProcessPaymentAsync(int orderId, bool success, CancellationToken ct = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_ProcessOrderPayment @OrderId={orderId}, @Success={success}", ct);

    public Task CancelOrderAsync(int orderId, CancellationToken ct = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_CancelOrder @OrderId={orderId}", ct);

    public async Task<List<OrderReportRow>> GetOrderReportAsync(DateTime startDate, DateTime endDate, CancellationToken ct = default)
    {
        return await _db.Database
            .SqlQuery<OrderReportRow>($"EXEC sp_GetOrderReport @StartDate={startDate}, @EndDate={endDate}")
            .ToListAsync(ct);
    }
}