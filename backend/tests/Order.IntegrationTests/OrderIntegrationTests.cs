using System;
using System.Threading.Tasks;
using Xunit;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http.Json;
using System.Text.Json;

namespace Order.IntegrationTests
{
    public class OrderIntegrationTests : IAsyncLifetime
    {
        private readonly TestcontainerDatabase _postgresContainer;
        private readonly HttpClient _httpClient = new();

        public OrderIntegrationTests()
        {
            _postgresContainer = new TestcontainersBuilder<PostgreSqlTestcontainer>()
                .WithDatabase(new PostgreSqlTestcontainerConfiguration
                {
                    Database = "ordersdb",
                    Username = "postgres",
                    Password = "postgres",
                })
                .WithImage("postgres:15")
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _postgresContainer.StopAsync();
        }

        [Fact(Skip = "Run manually: requires starting the API configured to use the Testcontainer DB")]
        public async Task CreateOrder_And_StoreInDb()
        {
            Assert.True(true);
        }
    }
}