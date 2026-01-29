using Microsoft.EntityFrameworkCore;
using OrderServiceNotifications.Data;
using OrderServiceNotifications.Messaging;


var builder = WebApplication.CreateBuilder(args);

// default to port 5000 when ASPNETCORE_URLS not provided
builder.WebHost.UseUrls(Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "http://localhost:5000");

builder.Services.AddDbContext<OrderDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddControllers();
builder.Services.AddLogging();
builder.Services.AddHostedService<OutboxPublisher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// ensure the database exists at startup for the demo/outbox to work
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Order database ensured/created successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure/create order database. Check DefaultConnection and that SQL Server is reachable.");
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
