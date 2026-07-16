// Models/AuditLog.cs
namespace Week6.Models;

public class AuditLog
{
    public long AuditLogId { get; set; }
    public string TableName { get; set; } = default!;
    public string Action { get; set; } = default!;
    public int RecordId { get; set; }
    public string UserId { get; set; } = default!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}