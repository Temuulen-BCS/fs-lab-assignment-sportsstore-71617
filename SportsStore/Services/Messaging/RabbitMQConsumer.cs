using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SportsStore.Services;
using System.Text;
using System.Text.Json;

namespace SportsStore.Services.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public RabbitMQConsumer(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var hostName = _configuration["RabbitMQ:HostName"] ?? "localhost";

            while (!stoppingToken.IsCancellationRequested)
            {
                IConnection? connection = null;
                IModel? channel = null;

                try
                {
                    var factory = new ConnectionFactory
                    {
                        HostName = hostName
                    };

                    connection = factory.CreateConnection();
                    channel = connection.CreateModel();

                    channel.QueueDeclare(
                        queue: "orderQueue",
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    Console.WriteLine("Connected to RabbitMQ.");

                    var consumer = new EventingBasicConsumer(channel);

                    consumer.Received += (model, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);

                        Console.WriteLine($"Order received: {message}");

                        try
                        {
                            var order = JsonSerializer.Deserialize<OrderViewDto>(message);

                            if (order != null)
                            {
                                using var scope = _serviceProvider.CreateScope();
                                var store = scope.ServiceProvider.GetRequiredService<OrderMemoryStore>();

                                store.UpdateStatus(order.Id, "Processed");

                                Console.WriteLine($"Order {order.Id} processed.");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error processing order: {ex.Message}");
                        }
                    };

                    channel.BasicConsume(
                        queue: "orderQueue",
                        autoAck: true,
                        consumer: consumer);

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"RabbitMQ not ready yet: {ex.Message}");
                    await Task.Delay(5000, stoppingToken);
                }
                finally
                {
                    try { channel?.Close(); } catch { }
                    try { connection?.Close(); } catch { }
                }
            }
        }
    }
}