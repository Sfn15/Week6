using Microsoft.EntityFrameworkCore;

namespace Week6.Data;

public record AuditLogRow(string TableName, string Action, int RecordId, string UserId, DateTime Timestamp);

public interface IAuditRepository
{
    Task<List<AuditLogRow>> GetAuditReportAsync(DateTime startDate, DateTime endDate, string? tableName, CancellationToken ct = default);
    Task<int> CleanupOldAuditDataAsync(int retentionDays, CancellationToken ct = default);
}

public class AuditRepository : IAuditRepository
{
    private readonly Week6DbContext _db;
    public AuditRepository(Week6DbContext db) => _db = db;

    public async Task<List<AuditLogRow>> GetAuditReportAsync(DateTime startDate, DateTime endDate, string? tableName, CancellationToken ct = default)
    {
        return await _db.Database
            .SqlQuery<AuditLogRow>($"EXEC sp_GetAuditReport @StartDate={startDate}, @EndDate={endDate}, @TableName={tableName}")
            .ToListAsync(ct);
    }

    public async Task<int> CleanupOldAuditDataAsync(int retentionDays, CancellationToken ct = default)
    {
        var connection = (Microsoft.Data.SqlClient.SqlConnection)_db.Database.GetDbConnection();
        var wasClosed = connection.State == System.Data.ConnectionState.Closed;
        if (wasClosed) await connection.OpenAsync(ct);

        try
        {
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "sp_CleanupOldAuditData";
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.Parameters.Add(new Microsoft.Data.SqlClient.SqlParameter("@RetentionDays", retentionDays));
            var outputParam = new Microsoft.Data.SqlClient.SqlParameter("@RowsDeleted", System.Data.SqlDbType.Int)
                { Direction = System.Data.ParameterDirection.Output };
            cmd.Parameters.Add(outputParam);

            await cmd.ExecuteNonQueryAsync(ct);
            return (int)outputParam.Value;
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }
}