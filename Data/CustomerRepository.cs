using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Week6.Models;

namespace Week6.Data;
// In CustomerRepository.cs, add:
public record CustomerRow(int CustomerId, string Email, string FirstName, string LastName, DateTime CreatedAt);
public record CustomerSearchRow(int CustomerId, string Email, string FirstName, string LastName, DateTime CreatedAt, int TotalCount);
public interface ICustomerRepository
{
    Task<int> CreateCustomerAsync(string email, string firstName, string lastName, CancellationToken ct = default);
    Task UpdateCustomerAsync(int customerId, string firstName, string lastName, string email, CancellationToken ct = default);
    Task SoftDeleteCustomerAsync(int customerId, CancellationToken ct = default);
    Task<List<CustomerSearchRow>> SearchCustomersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct = default);
    // ICustomerRepository
    Task<CustomerRow?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default);
}

public class CustomerRepository : ICustomerRepository
{
    private readonly Week6DbContext _db;

    public CustomerRepository(Week6DbContext db) => _db = db;

    public async Task<int> CreateCustomerAsync(string email, string firstName, string lastName, CancellationToken ct = default)
    {
        var connection = (SqlConnection)_db.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync(ct);

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_CreateCustomer";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.Add(new SqlParameter("@Email", email));
            cmd.Parameters.Add(new SqlParameter("@FirstName", firstName));
            cmd.Parameters.Add(new SqlParameter("@LastName", lastName));
            var outputParam = new SqlParameter("@CustomerId", System.Data.SqlDbType.Int) { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outputParam);

            await cmd.ExecuteNonQueryAsync(ct);
            return (int)outputParam.Value;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }

    public Task UpdateCustomerAsync(int customerId, string firstName, string lastName, string email, CancellationToken ct = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync(
            $"EXEC sp_UpdateCustomer @CustomerId={customerId}, @FirstName={firstName}, @LastName={lastName}, @Email={email}", ct);

    public Task SoftDeleteCustomerAsync(int customerId, CancellationToken ct = default) =>
        _db.Database.ExecuteSqlInterpolatedAsync($"EXEC sp_SoftDeleteCustomer @CustomerId={customerId}", ct);

    public async Task<List<CustomerSearchRow>> SearchCustomersAsync(string? searchTerm, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        return await _db.Database
            .SqlQuery<CustomerSearchRow>($"EXEC sp_SearchCustomers @SearchTerm={searchTerm}, @PageNumber={pageNumber}, @PageSize={pageSize}")
            .ToListAsync(ct);
    }

    private static async Task<IAsyncDisposable> OpenAsync(SqlConnection connection, CancellationToken ct)
    {
        await connection.OpenAsync(ct);
        return NoopAsyncDisposable.Instance; // connection stays open, managed by DbContext lifetime
    }
    public async Task<CustomerRow?> GetCustomerByIdAsync(int customerId, CancellationToken ct = default)
    {
        var results = await _db.Database
            .SqlQuery<CustomerRow>($"EXEC sp_GetCustomerById @CustomerId={customerId}")
            .ToListAsync(ct);

        return results.FirstOrDefault();
    }
}

file sealed class NoopAsyncDisposable : IAsyncDisposable
{
    public static readonly NoopAsyncDisposable Instance = new();
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}