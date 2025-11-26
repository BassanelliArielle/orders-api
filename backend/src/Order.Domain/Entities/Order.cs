using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace Order.Domain.Entities
{
    public enum OrderStatus { Pendente, Processando, Finalizado }

    public class Order
    {
        [Key]
        public Guid Id { get; set; }
        [Required]
        public string Cliente { get; set; }
        [Required]
        public string Produto { get; set; }
        [Column(TypeName = "numeric(18,2)")]
        public decimal Valor { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pendente;
        public DateTime DataCriacao { get; set; } = DateTime.UtcNow;

        public List<OrderStatusHistory> StatusHistory { get; set; } = new();
    }

    public class OrderStatusHistory
    {
        [Key]
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public OrderStatus Status { get; set; }
        public DateTime When { get; set; } = DateTime.UtcNow;
    }
}
