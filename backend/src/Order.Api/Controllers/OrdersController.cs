using Microsoft.AspNetCore.Mvc;
using Order.Api.Infrastructure;
using Order.Domain.Entities;
using Azure.Messaging.ServiceBus;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;

[ApiController]
[Route("orders")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _db;
    private readonly ServiceBusClient _sbClient;

    public OrdersController(OrderDbContext db, ServiceBusClient sbClient = null)
    {
        _db = db;
        _sbClient = sbClient;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var order = new Order { Id = Guid.NewGuid(), Cliente = dto.Cliente, Produto = dto.Produto, Valor = dto.Valor };
        _db.Orders.Add(order);
        _db.OrderStatusHistories.Add(new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = order.Id, Status = order.Status });
        await _db.SaveChangesAsync();

        if (_sbClient != null)
        {
            var sender = _sbClient.CreateSender("orders");
            var body = JsonSerializer.Serialize(new { order.Id, order.Cliente, order.Produto, order.Valor, order.DataCriacao });
            var msg = new ServiceBusMessage(body)
            {
                CorrelationId = order.Id.ToString()
            };
            msg.ApplicationProperties["EventType"] = "OrderCreated";
            await sender.SendMessageAsync(msg);
        }

        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var list = await _db.Orders.ToListAsync();
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        var history = await _db.OrderStatusHistories.Where(h => h.OrderId == id).OrderBy(h => h.When).ToListAsync();
        return Ok(new { order, history });
    }
}

public record CreateOrderDto(string Cliente, string Produto, decimal Valor);
