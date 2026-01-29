using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Consumers;

var builder = WebApplication.CreateBuilder(args);

// default to port 5001 when ASPNETCORE_URLS not provided so local runs don't conflict
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5001");

// rely on ASPNETCORE_URLS (or docker port mapping) rather than hard-coding a URL here
// this keeps runtime behavior consistent with how OrderService runs in Docker
// (Dockerfile sets ASPNETCORE_URLS=http://+:80 when running in container)

builder.Services.AddDbContext<NotificationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("NotificationConnection")));

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHostedService<OrderCreatedConsumer>();

var app = builder.Build();

// ensure database created (simple approach for demo)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Notification database ensured/created successfully.");
    }
    catch (Exception ex)
    {
        // Log detailed error to help diagnose DB connectivity issues (e.g., wrong connection string, auth)
        logger.LogError(ex, "Failed to ensure/create notification database. Check NotificationConnection and that SQL Server is reachable.");
        // rethrow to stop startup if desired, or continue (we'll continue to allow the app to run for diagnostics)
        // throw;
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

// simple root endpoint to verify service is up
app.MapGet("/", () => Results.Text("NotificationService running"));

app.Run();
