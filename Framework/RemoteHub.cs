#define VERBOSE_LOGGING

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace IoStorm
{
    public class RemoteHub : IDisposable
    {
        private Qlue.Logging.ILog log;
        private string hostName;
        private ConnectionFactory factory;
        private Dictionary<string, IModel> exchanges;
        private IoStorm.Serializer serializer;
        private IConnection connection;
        private string ourDeviceId;
        private IModel rpcModel;
        private QueueDeclareOk replyQueue;

        public RemoteHub(Qlue.Logging.ILogFactory logFactory, string hostName, string ourDeviceId)
        {
            this.log = logFactory.GetLogger("RemoteHub");
            this.hostName = hostName;
            this.ourDeviceId = ourDeviceId;

            this.exchanges = new Dictionary<string, IModel>();

            this.factory = new ConnectionFactory
            {
                HostName = this.hostName,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = 4000
            };

            this.serializer = new Serializer();
        }

        private IModel GetFanoutModel(string exchangeName)
        {
            IModel model;

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

            lock (this.exchanges)
            {
                if (!this.exchanges.TryGetValue(exchangeName, out model))
                {
                    // Create new
                    model = this.connection.CreateModel();
                    model.ExchangeDeclare(exchangeName, "fanout");

                    this.exchanges.Add(exchangeName, model);
                }
            }

            return model;
        }

        private IModel GetRpcModel()
        {
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

            return connection.CreateModel();
        }

        public void Dispose()
        {
            if (this.exchanges != null)
            {
                lock (this.exchanges)
                {
                    foreach (var channel in this.exchanges.Values)
                    {
                        channel.Close();

                        channel.Dispose();
                    }

                    this.exchanges.Clear();
                }

                this.exchanges = null;
            }

            if (this.connection != null)
            {
                this.connection.Dispose();

                this.connection = null;
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

            var channel = GetFanoutModel(channelName);
            string message = this.serializer.SerializeObject(busPayload);

            var body = Encoding.UTF8.GetBytes(message);

            channel.BasicPublish(channelName, string.Empty, properties, body);

#if VERBOSE_LOGGING
            this.log.Trace("Sent {0} bytes", body.Length);
#endif
        }

        public Payload.IPayload SendRpc(string channelName, Payload.IPayload payload)
        {
//FIXME
            IBasicProperties properties;
            var busPayload = GenerateBusMessage(payload, out properties);

            if (this.rpcModel == null)
                this.rpcModel = GetRpcModel();
            if (this.replyQueue == null)
                this.replyQueue = this.rpcModel.QueueDeclare();

            string message = this.serializer.SerializeObject(busPayload);

            var body = Encoding.UTF8.GetBytes(message);

            this.rpcModel.BasicPublish(string.Empty, channelName, properties, body);

#if VERBOSE_LOGGING
            this.log.Trace("Sent {0} bytes", body.Length);
#endif

            return null;
        }

        public void Receiver(string channelName, CancellationToken cancelToken, IObserver<Payload.BusPayload> bus)
        {
            var channel = GetFanoutModel(channelName);

            var queueName = channel.QueueDeclare();
            channel.QueueBind(queueName, channelName, string.Empty);

            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(queueName, true, consumer);

            this.log.Debug("Waiting for messages");
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    BasicDeliverEventArgs result;
                    if (consumer.Queue.Dequeue(300, out result))
                    {
                        var body = result.Body;

#if VERBOSE_LOGGING
                        this.log.Trace("Received {0} bytes", body.Length);
#endif

                        var payload = this.serializer.DeserializeString(Encoding.UTF8.GetString(body)) as Payload.BusPayload;

                        if (payload != null)
                        {
                            if (payload.OriginDeviceId == ourDeviceId)
                                // Ignore our own messages
                                continue;

                            bus.OnNext(payload);
                        }
                    }
                }
                catch (System.IO.EndOfStreamException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Ignore
                    this.log.WarnException("Receive remote hub payload", ex);
                }
            }
        }

        public void ReceiverRPC(string channelName, CancellationToken cancelToken, IObserver<Payload.RPCPayload> bus)
        {
            var channel = GetRpcModel();

            channel.QueueDeclare(channelName, false, false, false, null);
            channel.BasicQos(0, 1, false);

            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(channelName, false, consumer);

            this.log.Debug("Waiting for messages");
            while (!cancelToken.IsCancellationRequested)
            {
                try
                {
                    BasicDeliverEventArgs result;
                    if (consumer.Queue.Dequeue(300, out result))
                    {
                        var body = result.Body;

#if VERBOSE_LOGGING
                        this.log.Trace("Received {0} bytes", body.Length);
#endif

                        var payload = this.serializer.DeserializeString(Encoding.UTF8.GetString(body)) as Payload.IPayload;

                        if (payload != null)
                        {
                            var rpcPayload = new Payload.RPCPayload
                            {
                                Request = payload
                            };

                            bus.NotifyOn(TaskPoolScheduler.Default).OnNext(rpcPayload);

                            if (rpcPayload.Response != null)
                            {
                                // Send response
                                var replyProps = channel.CreateBasicProperties();
                                replyProps.CorrelationId = result.BasicProperties.CorrelationId;

                                byte[] responseBody = Encoding.UTF8.GetBytes(this.serializer.SerializeObject(rpcPayload.Response));

                                channel.BasicPublish(string.Empty, result.BasicProperties.ReplyTo, replyProps, responseBody);
                                channel.BasicAck(result.DeliveryTag, false);
                            }
                        }
                    }
                }
                catch (System.IO.EndOfStreamException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    // Ignore
                    this.log.WarnException("Receive remote hub payload", ex);
                }
            }

            channel.Dispose();
        }
    }
}
