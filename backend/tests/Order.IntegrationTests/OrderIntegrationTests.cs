using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Xunit;
using System.IO;
using System.Text.Json;

namespace Order.IntegrationTests
{
    public class OrderIntegrationTests : IAsyncLifetime
    {
        private readonly TestcontainersContainer _postgresContainer;
        private WebApplicationFactory<Program> _factory;
        private HttpClient _client;

        public OrderIntegrationTests()
        {
            _postgresContainer = new TestcontainersBuilder<TestcontainersContainer>()
                .WithImage("postgres:15")
                .WithEnvironment("POSTGRES_DB", "ordersdb")
                .WithEnvironment("POSTGRES_USER", "postgres")
                .WithEnvironment("POSTGRES_PASSWORD", "postgres")
                .WithExposedPort(5432)
                .WithWaitStrategy(DotNet.Testcontainers.Configurations.Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _postgresContainer.StartAsync();
            var mappedPort = _postgresContainer.GetMappedPublicPort(5432);
            var conn = $"Host=localhost;Port={mappedPort};Database=ordersdb;Username=postgres;Password=postgres";

            _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((ctx, conf) =>
                {
                    var dict = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "ConnectionStrings:DefaultConnection", conn },
                        { "ServiceBus__ConnectionString", "" }
                    };
                    conf.AddInMemoryCollection(dict);
                });
            });

            _client = _factory.CreateClient();
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            _factory?.Dispose();
            await _postgresContainer.StopAsync();
        }

        [Fact]
        public async Task CreateOrder_EndToEnd()
        {
            var dto = new { Cliente = "Test", Produto = "Item", Valor = 10.5 };
            var resp = await _client.PostAsJsonAsync("/orders", dto);
            resp.EnsureSuccessStatusCode();
            var created = await resp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(created.GetProperty("id").GetGuid() != Guid.Empty);
        }
    }
}
