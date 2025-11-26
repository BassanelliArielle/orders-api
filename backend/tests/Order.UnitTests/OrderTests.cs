using System;
using Xunit;
using Order.Domain.Entities;

namespace Order.UnitTests
{
    public class OrderTests
    {
        [Fact]
        public void NewOrder_HasPendingStatusAndCreationDate()
        {
            var order = new Order { Id = Guid.NewGuid(), Cliente = "Jhon", Produto = "Product", Valor = 10m };
            Assert.Equal(OrderStatus.Pendente, order.Status);
            Assert.True((DateTime.UtcNow - order.DataCriacao).TotalSeconds < 60);
        }
    }
}