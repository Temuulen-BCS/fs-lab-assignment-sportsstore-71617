using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SportsStore.Models.DTOs;
using SportsStore.Services;
using System.Text;
using System.Text.Json;

namespace SportsStore.Services.Messaging
{
    public class RabbitMQConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RabbitMQConsumer> _logger;

        public RabbitMQConsumer(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<RabbitMQConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
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

                    _logger.LogInformation("Connected to RabbitMQ queue {Queue}.", "orderQueue");

                    var consumer = new EventingBasicConsumer(channel);

                    consumer.Received += (_, ea) =>
                    {
                        var body = ea.Body.ToArray();
                        var payload = Encoding.UTF8.GetString(body);
                        _ = Task.Run(() => ProcessMessageAsync(payload, stoppingToken), stoppingToken);
                    };

                    channel.BasicConsume(
                        queue: "orderQueue",
                        autoAck: true,
                        consumer: consumer);

                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "RabbitMQ not ready yet. Retrying in 5 seconds.");
                    await Task.Delay(5000, stoppingToken);
                }
                finally
                {
                    try { channel?.Close(); } catch { }
                    try { connection?.Close(); } catch { }
                }
            }
        }

        private async Task ProcessMessageAsync(string payload, CancellationToken cancellationToken)
        {
            try
            {
                var workflowMessage = JsonSerializer.Deserialize<OrderWorkflowMessage>(payload);
                if (workflowMessage == null)
                {
                    _logger.LogWarning("Received an empty or invalid workflow message.");
                    return;
                }

                using var scope = _serviceProvider.CreateScope();
                var store = scope.ServiceProvider.GetRequiredService<OrderMemoryStore>();
                var inventoryService = scope.ServiceProvider.GetRequiredService<InventoryService>();
                var paymentService = scope.ServiceProvider.GetRequiredService<PaymentworkflowService>();
                var shippingService = scope.ServiceProvider.GetRequiredService<ShippingService>();

                var order = store.Upsert(workflowMessage);
                store.AddLog(order.Id, "MessageConsumer", "Workflow message consumed from RabbitMQ.");
                store.UpdateWorkflowState(order.Id, status: "Processing");

                _logger.LogInformation(
                    "Workflow message consumed. OrderId={OrderId} CorrelationId={CorrelationId}",
                    workflowMessage.OrderId,
                    workflowMessage.CorrelationId);

                var inventoryRecord = await inventoryService.ValidateAsync(order, cancellationToken);
                store.UpdateWorkflowState(
                    order.Id,
                    status: inventoryRecord.Success ? "Inventory Confirmed" : "Failed",
                    inventoryStatus: inventoryRecord.Success ? "Validated" : "Failed");
                store.AddLog(order.Id, "InventoryService", inventoryRecord.Message, inventoryRecord.Success ? "Information" : "Warning");

                if (!inventoryRecord.Success)
                {
                    return;
                }

                var paymentRecord = await paymentService.ProcessAsync(order, cancellationToken);
                store.UpdateWorkflowState(
                    order.Id,
                    status: paymentRecord.Success ? "Payment Approved" : "Failed",
                    paymentStatus: paymentRecord.Success ? "Approved" : "Failed");
                store.AddLog(order.Id, "PaymentworkflowService", paymentRecord.Message, paymentRecord.Success ? "Information" : "Warning");

                if (!paymentRecord.Success)
                {
                    return;
                }

                var shipmentRecord = await shippingService.CreateShipmentAsync(order, cancellationToken);
                store.UpdateWorkflowState(
                    order.Id,
                    status: shipmentRecord.Success ? "Completed" : "Failed",
                    shippingStatus: shipmentRecord.Success ? "Created" : "Failed",
                    shipmentReference: shipmentRecord.ShipmentReference);
                store.AddLog(order.Id, "ShippingService", shipmentRecord.Message, shipmentRecord.Success ? "Information" : "Warning");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing workflow message.");
            }
        }
    }
}
