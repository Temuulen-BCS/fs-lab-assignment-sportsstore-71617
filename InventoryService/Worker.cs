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
            queue: "inventoryQueue",
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        var consumer = new EventingBasicConsumer(channel);

        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            Console.WriteLine($"[Inventory] Received: {message}");

            var order = System.Text.Json.JsonSerializer.Deserialize<Order>(message);

            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<StoreDbContext>();

            var existingOrder = await db.Orders
                .FirstOrDefaultAsync(o => o.OrderID == order.OrderID);

            if (existingOrder != null)
            {
                existingOrder.Status = OrderStatus.InventoryConfirmed;
                await db.SaveChangesAsync();
            }

            var paymentMessage = System.Text.Json.JsonSerializer.Serialize(order);
            var paymentBody = Encoding.UTF8.GetBytes(paymentMessage);

            channel.BasicPublish(exchange: "", routingKey: "paymentQueue", basicProperties: null, body: paymentBody);

            Console.WriteLine("[Inventory] Sent to paymentQueue");
        };

        channel.BasicConsume(queue: "inventoryQueue", autoAck: true, consumer: consumer);

        return Task.CompletedTask;
    }
}