using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Order.Api.Infrastructure;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;
using System.IO;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? builder.Configuration["ConnectionStrings:DefaultConnection"];
var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"] ?? builder.Configuration["ServiceBus__ConnectionString"];
var frontendUrl = builder.Configuration["FRONTEND_URL"] ?? Environment.GetEnvironmentVariable("FRONTEND_URL") ?? "http://localhost:3000";

builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c => {
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = System.IO.Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (System.IO.File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});


builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(frontendUrl.Split(','))
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


if (!string.IsNullOrEmpty(serviceBusConnection))
{
    builder.Services.AddSingleton((sp) => new ServiceBusClient(serviceBusConnection));
}
else
{
    builder.Services.AddSingleton<ServiceBusClient>(sp => null);
}

builder.Services.AddHostedService<OutboxDispatcher>();
builder.Services.AddSignalR();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>("postgres")
    .AddCheck<ServiceBusHealthCheck>("servicebus");

builder.Services.AddSingleton<ServiceBusHealthCheck>();

var app = builder.Build();

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    var applied = false;
    var retries = 5;
    for (int i=0;i<retries;i++)
    {
        try
        {
            db.Database.Migrate();
            applied = true;
            break;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Migration attempt {i+1} failed: {ex.Message}");
            System.Threading.Thread.Sleep(2000);
        }
    }
    if (!applied)
    {
        Console.WriteLine("Applying SQL schema fallback (init_schema.sql)");
        try
        {
            var sqlPath = Path.Combine(AppContext.BaseDirectory, "../../../../infra/sql/init_schema.sql");
            if (!File.Exists(sqlPath)) sqlPath = Path.Combine(AppContext.BaseDirectory, "infra/sql/init_schema.sql");
            var sql = File.ReadAllText(sqlPath);
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            cmd.ExecuteNonQuery();
            Console.WriteLine("SQL schema applied");
        }
        catch (Exception ex)
        {
            Console.WriteLine("SQL fallback failed: " + ex.Message);
        }
    }
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapHub<Order.Api.Hubs.OrdersHub>("/ordersHub");

app.MapPost("/internal/notify", async (Microsoft.AspNetCore.Http.HttpRequest req, Microsoft.AspNetCore.SignalR.IHubContext<Order.Api.Hubs.OrdersHub> hub) => {
    try
    {
        using var sr = new System.IO.StreamReader(req.Body);
        var body = await sr.ReadToEndAsync();

        await hub.Clients.All.SendAsync("OrderUpdated", System.Text.Json.JsonSerializer.Deserialize<object>(body));
        return Results.Ok();
    }
    catch(Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

app.Run();
