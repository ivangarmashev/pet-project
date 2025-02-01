namespace OneMoreSpreadSearcher;

using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Threading.Tasks;

public class RabbitMqService : IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private const string QueueName = "balancesQueue";

    public RabbitMqService()
    {
        var factory = new ConnectionFactory()
        {
            HostName = "localhost"
        };

        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;

        _channel.QueueDeclareAsync(queue: QueueName,
                              durable: true,
                              exclusive: false,
                              autoDelete: false,
                              arguments: null).Wait();
    }

    public async void SendMessage(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(exchange: "",
                              routingKey: QueueName,
                              basicProperties: new BasicProperties(), // Создаем новый экземпляр BasicProperties
                              body: body,
                              mandatory: false);
    }
    
    public async void SendSpreadsMessage(string message)
    {
        var body = Encoding.UTF8.GetBytes(message);

        await _channel.BasicPublishAsync(exchange: "",
                              routingKey: "spreadsQueue",
                              basicProperties: new BasicProperties(), // Создаем новый экземпляр BasicProperties
                              body: body,
                              mandatory: false);
    }

    public async Task<string> ReceiveMessageAsync()
    {
        var tcs = new TaskCompletionSource<string>();
        var consumer = new AsyncEventingBasicConsumer(_channel);

        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            tcs.TrySetResult(message);

            // Подтверждение получения сообщения
            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);

            await Task.Yield(); // Асинхронное завершение
        };

        await _channel.BasicConsumeAsync(queue: QueueName,
                              autoAck: false, // Используем подтверждения
                              consumer: consumer);

        return await tcs.Task;
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}