using Microsoft.Extensions.Diagnostics.HealthChecks;
using Azure.Messaging.ServiceBus;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace Order.Api.Health
{
    public class ServiceBusHealthCheck : IHealthCheck
    {
        private readonly ServiceBusClient _client;
        public ServiceBusHealthCheck(ServiceBusClient client) => _client = client;

        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                if (_client == null) return HealthCheckResult.Unhealthy("ServiceBus not configured");
                await using var sender = _client.CreateSender("orders");
                return HealthCheckResult.Healthy("Service Bus reachable");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy(ex.Message);
            }
        }
    }
}
