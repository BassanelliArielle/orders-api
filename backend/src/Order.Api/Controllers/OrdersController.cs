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

    public OrdersController(OrderDbContext db)
    {
        _db = db;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var order = new Order { Id = Guid.NewGuid(), Cliente = dto.Cliente, Produto = dto.Produto, Valor = dto.Valor };
        _db.Orders.Add(order);
        _db.OrderStatusHistories.Add(new OrderStatusHistory { Id = Guid.NewGuid(), OrderId = order.Id, Status = order.Status });

        var outbox = new OutboxMessage
        {
            Destination = "orders",
            Payload = JsonSerializer.Serialize(new { order.Id, order.Cliente, order.Produto, order.Valor, order.DataCriacao }),
            CorrelationId = order.Id.ToString(),
            EventType = "OrderCreated"
        };
        _db.OutboxMessages.Add(outbox);

        await _db.SaveChangesAsync();

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
