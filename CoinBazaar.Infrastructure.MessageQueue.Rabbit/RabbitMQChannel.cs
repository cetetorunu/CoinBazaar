using Camunda.Api.Client;
using Camunda.Api.Client.ExternalTask;
using Camunda.Api.Client.Message;
using MediatR;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CoinBazaar.Infrastructure.MessageQueue.Rabbit
{
    internal class RabbitMQChannel : Channel, IChannel, IDisposable
    {
        protected const string VirtualHost = "CUSTOM_HOST";
        protected readonly string LoggerExchange = $"{VirtualHost}.LoggerExchange";
        protected readonly string LoggerQueue = $"{VirtualHost}.log.message";
        protected const string LoggerQueueAndExchangeRoutingKey = "log.message";



        protected readonly string ExchangeName = "CUSTOM_HOST.LoggerExchange";
        protected readonly string RoutingKeyName = "log.message";
        protected readonly string AppId = "LogProducer";


        protected IModel Channel { get; private set; }

        private readonly RabbitMQ.Client.IConnection _connection;
        private readonly IMediator _mediator;



        public RabbitMQChannel(RabbitMQ.Client.IConnection connection, IMediator mediator)
        {
            _connection = connection;

            this._mediator = mediator;
        }

        public override Task StartListen()
        {
            if (Channel == null || Channel.IsOpen == false)
            {
                Channel = _connection.CreateModel();
                Channel.ExchangeDeclare(exchange: LoggerExchange, type: "direct", durable: true, autoDelete: false);
                Channel.QueueDeclare(queue: LoggerQueue, durable: false, exclusive: false, autoDelete: false);
                Channel.QueueBind(queue: LoggerQueue, exchange: LoggerExchange, routingKey: LoggerQueueAndExchangeRoutingKey);
            }


            var consumer = new AsyncEventingBasicConsumer(Channel);
            consumer.Received += Consumer_Received;


            return Task.CompletedTask;
        }

        private async Task Consumer_Received(object sender, BasicDeliverEventArgs @event)
        {
            IBasicProperties basicProperties = @event.BasicProperties;
            var body = Encoding.UTF8.GetString(@event.Body.ToArray());
            MethodInfo method = typeof(RabbitMQChannel).GetMethod("BindObject");
            MethodInfo generic = method.MakeGenericMethod(Type.GetType(basicProperties.ContentType));
            var result = generic.Invoke(this, new object[] { body });
            var mResult = await _mediator.Send(result);

            Channel.BasicAck(@event.DeliveryTag, false);

            //Console.WriteLine("Message received by the event based consumer. Check the debug window for details.");
            //Debug.WriteLine(string.Concat("Message received from the exchange ", @event.Exchange));
            //Debug.WriteLine(string.Concat("Content type: ", basicProperties.ContentType));
            //Debug.WriteLine(string.Concat("Consumer tag: ", @event.ConsumerTag));
            //Debug.WriteLine(string.Concat("Delivery tag: ", @event.DeliveryTag));
            //Debug.WriteLine(string.Concat("Message: ", Encoding.UTF8.GetString(@event.Body.ToArray())));


        }

        public T BindObject<T>(string body)
        {
            return JsonConvert.DeserializeObject<T>(body);
        }

        public override async Task StopListen()
        {
            Channel?.Close();
            _connection?.Close();
            await Task.CompletedTask;
        }

        public override async Task Publish(INotification @event)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(@event));
                var properties = Channel.CreateBasicProperties();
                properties.AppId = AppId;
                properties.ContentType = "application/json";
                properties.DeliveryMode = 1; // Doesn't persist to disk
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                Channel.BasicPublish(exchange: ExchangeName, routingKey: RoutingKeyName, body: body, basicProperties: properties);

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
            }
        }


        public override void Dispose()
        {
            try
            {
                Channel?.Close();
                Channel?.Dispose();
                Channel = null;

                _connection?.Close();
                _connection?.Dispose();
            }
            catch (Exception ex)
            {
            }
        }
    }
}