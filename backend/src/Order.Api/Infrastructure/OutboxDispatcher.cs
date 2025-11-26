using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System;
using Microsoft.Extensions.DependencyInjection;
using Azure.Messaging.ServiceBus;
using System.Linq;

namespace Order.Api.Infrastructure
{
    public class OutboxDispatcher : BackgroundService
    {
        private readonly IServiceProvider _sp;
        private readonly ServiceBusClient _sbClient;
        private readonly TimeSpan _interval = TimeSpan.FromSeconds(3);

        public OutboxDispatcher(IServiceProvider sp, ServiceBusClient sbClient = null)
        {
            _sp = sp;
            _sbClient = sbClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _sp.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    var pending = db.OutboxMessages
                        .Where(o => !o.Dispatched)
                        .OrderBy(o => o.OccurredAt)
                        .Take(20)
                        .ToList();

                    foreach (var msg in pending)
                    {
                        try
                        {
                            if (_sbClient == null)
                            {
                                msg.DispatchAttempts++;
                                db.SaveChanges();
                                continue;
                            }

                            var sender = _sbClient.CreateSender(msg.Destination);
                            var sbMsg = new ServiceBusMessage(msg.Payload)
                            {
                                CorrelationId = msg.CorrelationId
                            };
                            sbMsg.ApplicationProperties["EventType"] = msg.EventType;

                            sender.SendMessageAsync(sbMsg).GetAwaiter().GetResult();

                            msg.Dispatched = true;
                            msg.DispatchedAt = DateTime.UtcNow;
                            msg.DispatchAttempts++;
                            db.SaveChanges();
                        }
                        catch (Exception)
                        {
                            msg.DispatchAttempts++;
                            db.SaveChanges();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
