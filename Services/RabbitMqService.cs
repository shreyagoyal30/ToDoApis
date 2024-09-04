using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using ToDoApi.Models;

namespace TodoApi.Services 
{
    public class RabbitMqServiceProducer
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqServiceProducer()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" }; // Use your RabbitMQ server address
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: "todo_queue",
                                  durable: false,
                                  exclusive: false,
                                  autoDelete: false,
                                  arguments: null);
        }

        public void SendMessage(ToDo todo)
        {
            var message = JsonConvert.SerializeObject(new { Event = "TodoCreated", Todo = todo });
            var body = Encoding.UTF8.GetBytes(message);

            _channel.BasicPublish(exchange: "",
                                  routingKey: "todo_queue",
                                  basicProperties: null,
                                  body: body);
        }

        ~RabbitMqServiceProducer()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }

    public class RabbitMqServiceConsumer : BackgroundService
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMqServiceConsumer()
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(queue: "todo_queue",
                                 durable: false,
                                 exclusive: false,
                                 autoDelete: false,
                                 arguments: null);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);//allows the message to consume from a queue
            consumer.Received += (model, ea) =>
            {
                var body = ea.Body.ToArray();//retrive byte array from the ea
                var message = Encoding.UTF8.GetString(body);
                var todoCreatedEvent = JsonConvert.DeserializeObject<TodoCreatedEvent>(message);

                // Display the received Todo item details
                Console.WriteLine($"Todo Created: {todoCreatedEvent.Todo.Title}, Due: {todoCreatedEvent.Todo.DueDate}");
            };

            _channel.BasicConsume(queue: "todo_queue",
                                 autoAck: true,
                                 consumer: consumer);

            return Task.CompletedTask;
        }

        ~RabbitMqServiceConsumer()
        {
            _channel.Close();
            _connection.Close();
        }
    }

    public class TodoCreatedEvent
    {
        public string Event { get; set; }
        public ToDo Todo { get; set; }
    }
}
