using Microsoft.Extensions.Configuration;
using RabbitMQ.Client;
using System.Text;

namespace SportsStore.Services.Messaging
{
    public class RabbitMQService
    {
        private readonly ConnectionFactory _factory;

        public RabbitMQService(IConfiguration configuration)
        {
            var hostName = configuration["RabbitMQ:HostName"] ?? "localhost";

            _factory = new ConnectionFactory
            {
                HostName = hostName
            };
        }

        public void SendMessage(string queue, string message)
        {
            const int maxRetries = 5;
            const int delayMilliseconds = 3000;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    using var connection = _factory.CreateConnection();
                    using var channel = connection.CreateModel();

                    channel.QueueDeclare(
                        queue: queue,
                        durable: true,
                        exclusive: false,
                        autoDelete: false,
                        arguments: null);

                    var body = Encoding.UTF8.GetBytes(message);

                    channel.BasicPublish(
                        exchange: "",
                        routingKey: queue,
                        basicProperties: null,
                        body: body);

                    Console.WriteLine($"✅ Message sent to RabbitMQ queue '{queue}'.");
                    return;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ RabbitMQ send attempt {attempt} failed: {ex.Message}");

                    if (attempt == maxRetries)
                    {
                        throw;
                    }

                    Thread.Sleep(delayMilliseconds);
                }
            }
        }
    }
}