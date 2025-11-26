using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using Order.Api.Infrastructure;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
var serviceBusConnection = builder.Configuration["ServiceBus:ConnectionString"] ?? builder.Configuration["ServiceBus__ConnectionString"];

builder.Services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(connectionString));
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (!string.IsNullOrEmpty(serviceBusConnection))
{
    builder.Services.AddSingleton((sp) => new ServiceBusClient(serviceBusConnection));
}

builder.Services.AddHealthChecks()
    .AddDbContextCheck<OrderDbContext>()
    .AddCheck("servicebus", () =>
    {
        if (string.IsNullOrEmpty(serviceBusConnection)) return HealthCheckResult.Unhealthy("No ServiceBus configured");
        try { using var client = new ServiceBusClient(serviceBusConnection); return HealthCheckResult.Healthy(); }
        catch (Exception ex) { return HealthCheckResult.Unhealthy(ex.Message); }
    });

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.Run();
