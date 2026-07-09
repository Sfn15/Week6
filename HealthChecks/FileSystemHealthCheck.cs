using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Week6.HealthChecks;

public class FileSystemHealthCheck : IHealthCheck
{
    private readonly string _path;
    public FileSystemHealthCheck(string path = "App_Data") => _path = path;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            Directory.CreateDirectory(_path);
            var testFile = Path.Combine(_path,".healthcheck");
            File.WriteAllText(testFile, DateTime.UtcNow.ToString("o"));
            File.Delete(testFile);
            return Task.FromResult(HealthCheckResult.Healthy("File system writable"));
        } catch (Exception e)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("File system not writeable", e));
        }
    }
}