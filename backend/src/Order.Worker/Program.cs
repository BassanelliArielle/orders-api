using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Order.Api.Infrastructure;
using Order.Domain.Entities;
using System;
using System.Threading;
using System.Threading.Tasks;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddDbContext<OrderDbContext>(opt => opt.UseNpgsql(context.Configuration.GetConnectionString("DefaultConnection")));
        services.AddHostedService<OrderWorker>();
    })
    .Build();

await host.RunAsync();

public class OrderWorker : BackgroundService
{
    private readonly IServiceProvider _sp;
    private readonly ServiceBusClient _client;
    private ServiceBusProcessor _processor;
    private readonly HttpClient _http = new HttpClient();

    public OrderWorker(IServiceProvider sp)
    {
        _sp = sp;
        var conn = Environment.GetEnvironmentVariable("ServiceBus__ConnectionString") ?? Environment.GetEnvironmentVariable("AZURE_SERVICE_BUS_CONNECTION_STRING");
        if (!string.IsNullOrEmpty(conn))
        {
            _client = new ServiceBusClient(conn);
            _processor = _client.CreateProcessor("orders", new ServiceBusProcessorOptions { MaxConcurrentCalls = 1, AutoCompleteMessages = false });
            _processor.ProcessMessageAsync += ProcessMessageAsync;
            _processor.ProcessErrorAsync += ErrorHandler;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_processor != null)
        {
            await _processor.StartProcessingAsync(stoppingToken);
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs arg)
    {
        Console.WriteLine(arg.Exception.Message);
        return Task.CompletedTask;
    }

    private async Task ProcessMessageAsync(ProcessMessageEventArgs args)
    {
        var body = args.Message.Body.ToString();
        var correlation = args.Message.CorrelationId;
        var eventType = args.Message.ApplicationProperties.ContainsKey("EventType") ? args.Message.ApplicationProperties["EventType"]?.ToString() : null;
        var msgId = args.Message.MessageId;

        if (eventType != "OrderCreated")
        {
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        if (!Guid.TryParse(correlation, out var orderId))
        {
            await args.DeadLetterMessageAsync(args.Message, "InvalidCorrelationId", "CorrelationId is not a valid guid");
            return;
        }

        using var scope = _sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        using var tx = await db.Database.BeginTransactionAsync();
        var consumed = await db.ConsumedMessages.FindAsync(new object[] { msgId });
        if (consumed != null)
        {
            await tx.RollbackAsync();
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        db.ConsumedMessages.Add(new ConsumedMessage { MessageId = msgId, OrderId = orderId });
        await db.SaveChangesAsync();

        var order = await db.Orders.FindAsync(orderId);
        if (order == null)
        {
            await tx.CommitAsync();
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        if (order.Status != OrderStatus.Pendente)
        {
            await tx.CommitAsync();
            await args.CompleteMessageAsync(args.Message);
            return;
        }

        order.Status = OrderStatus.Processando;
        db.OrderStatusHistories.Add(new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = order.Id, Status = order.Status });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await Task.Delay(TimeSpan.FromSeconds(5));

        using var tx2 = await db.Database.BeginTransactionAsync();
        order.Status = OrderStatus.Finalizado;
        db.OrderStatusHistories.Add(new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = order.Id, Status = order.Status });
        await db.SaveChangesAsync();
        await tx2.CommitAsync();

        try
        {
            var payload = JsonSerializer.Serialize(new { id = order.Id, status = order.Status.ToString() });
            var apiBase = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://api:80";
            var resp = await _http.PostAsync(apiBase + "/internal/notify", new StringContent(payload, Encoding.UTF8, "application/json"));
        }
        catch { /* ignore */ }

        await args.CompleteMessageAsync(args.Message);
    }
}