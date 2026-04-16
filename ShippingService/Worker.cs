using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.EntityFrameworkCore;
using SportsStore.Models;

public class Worker : BackgroundService
{
    private readonly IConfiguration _config;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(IConfiguration config, IServiceScopeFactory scopeFactory)
    {
        _config = config;
        _scopeFactory = scopeFactory;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _config["RabbitMQ:HostName"] ?? "rabbitmq"
        };

        var connection = factory.CreateConnection();
        var channel = connection.CreateModel();

        channel.QueueDeclare(
            queue: "shippingQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"[Shipping] Received: {message}");

            var order = System.Text.Json.JsonSerializer.Deserialize<Order>(message);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

            var existingOrder = await db.Orders
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (existingOrder != null)
            {
                existingOrder.Status = OrderStatus.Completed;
                await db.SaveChangesAsync();
            }

            Console.WriteLine("[Shipping] Order completed");
        };

        channel.BasicConsume("shippingQueue", true, consumer);

        return Task.CompletedTask;
    }
}