﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Storm
{
    public class Amqp : IDisposable
    {
        private Qlue.Logging.ILog log;
        private string hostName;
        private ConnectionFactory factory;
        private Dictionary<string, IModel> channels;
        private Storm.Serializer serializer;
        private IConnection connection;
        private string ourDeviceId;

        public Amqp(Qlue.Logging.ILogFactory logFactory, string hostName, string ourDeviceId)
        {
            this.log = logFactory.GetLogger("Amqp");
            this.hostName = hostName;
            this.ourDeviceId = ourDeviceId;

            this.channels = new Dictionary<string, IModel>();

            this.factory = new ConnectionFactory
            {
                HostName = this.hostName,
                AutomaticRecoveryEnabled = true
            };

            this.serializer = new Serializer();
        }

        private IModel GetChannel(string channelName)
        {
            IModel channel;

            if (this.connection == null)
            {
                lock (this)
                {
                    if (this.connection == null)
                    {
                        this.connection = factory.CreateConnection();
                    }
                }
            }

            lock (this.channels)
            {
                if (!this.channels.TryGetValue(channelName, out channel))
                {
                    // Create new
                    channel = this.connection.CreateModel();
                    channel.ExchangeDeclare(channelName, "fanout");

                    this.channels.Add(channelName, channel);
                }
            }

            return channel;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.channels != null)
                {
                    lock (this.channels)
                    {
                        foreach (var channel in this.channels.Values)
                        {
                            channel.Dispose();
                        }

                        this.channels.Clear();
                    }

                    this.channels = null;
                }

                if (this.connection != null)
                {
                    this.connection.Dispose();

                    this.connection = null;
                }
            }
        }

        private Payload.BusPayload GenerateBusMessage(Payload.IPayload payload, out IBasicProperties properties)
        {
            var busPayload = new Payload.BusPayload
            {
                OriginDeviceId = this.ourDeviceId,
                Payload = payload
            };

            properties = new RabbitMQ.Client.Framing.BasicProperties
            {
                AppId = "Storm",
                MessageId = Guid.NewGuid().ToString("n"),
                ContentType = "application/json",
                Type = payload.GetType().FullName
            };

            return busPayload;
        }

        public void SendPayload(string channelName, Payload.IPayload payload)
        {
            IBasicProperties properties;
            var busPayload = GenerateBusMessage(payload, out properties);

            var channel = GetChannel(channelName);

            string message = this.serializer.SerializeObject(busPayload);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(channelName, string.Empty, properties, body);

            this.log.Trace("Sent {0} bytes", body.Length);
        }

        public void Receiver(string channelName, CancellationToken cancelToken, IObserver<Payload.IPayload> bus)
        {
            var channel = GetChannel(channelName);

            var queueName = channel.QueueDeclare();
            channel.QueueBind(queueName, channelName, string.Empty);

            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(queueName, true, consumer);

            this.log.Info("Waiting for messages");
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    BasicDeliverEventArgs result;
                    if (consumer.Queue.Dequeue(100, out result))
                    {
                        var body = result.Body;

                        this.log.Debug("Received {0} bytes", body.Length);

                        object obj = this.serializer.DeserializeString(Encoding.UTF8.GetString(body));

                        var payload = obj as Payload.BusPayload;

                        if (payload.OriginDeviceId == ourDeviceId)
                            // Ignore our own messages
                            continue;

                        if (payload != null)
                            bus.OnNext(payload.Payload);
                    }
                }
                catch (System.IO.EndOfStreamException)
                {
                    break;
                }
            }
        }
    }
}
