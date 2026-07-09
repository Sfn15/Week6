using Week6.Middleware;
using Week6.Services.Email;
using Week6.Services.Cleanup;
using Week6.HealthChecks;
using Week6.Services.Reports;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddSingleton<IEmailQueue, EmailQueue>();
builder.Services.AddHostedService<EmailProcessingBackgroundService>();
builder.Services.AddHostedService<DataCleanupBackgroundService>();

builder.Services.AddControllers();
builder.Services.AddHttpClient();
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("Default") ?? "Server=.;Database=Week6;Trusted_Connection=True;TrustServerCertificate=True;",
        name: "sqlserver")
    .AddCheck<FileSystemHealthCheck>("filesystem")
    .AddCheck<ExternalApiHealthCheck>("external-api");

builder.Services.AddHostedService<ReportGeneratorBackgroundService>();


var app = builder.Build();




// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<IpRateLimitingMiddleware>();
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.UseHttpsRedirection();

app.MapControllers();
app.MapHealthChecks("/health");
var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
