// Models/ErrorLog.cs
namespace Week6.Models;

public class ErrorLog
{
    public long ErrorLogId { get; set; }
    public int? ErrorNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProcedureName { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}