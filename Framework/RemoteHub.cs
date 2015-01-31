//#define VERBOSE_LOGGING

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
        protected class Waiter
        {
            private ManualResetEvent waitEvent;
            private Payload.IPayload response;

            public Waiter()
            {
                this.waitEvent = new ManualResetEvent(false);
            }

            public Payload.IPayload GetResponse(TimeSpan timeout)
            {
                if (!this.waitEvent.WaitOne(timeout))
                    throw new TimeoutException("No response");

                return this.response;
            }

            public void ReplyReceived(Payload.IPayload payload)
            {
                this.response = payload;

                this.waitEvent.Set();
            }
        }

        private Qlue.Logging.ILog log;
        private string hostName;
        private ConnectionFactory factory;
        private Dictionary<string, IModel> exchanges;
        private IoStorm.Serializer serializer;
        private IConnection connection;
        private string ourDeviceId;
        private IModel rpcModel;
        private string rpcQueueName;
        private QueueDeclareOk rpcReplyQueue;
        private Dictionary<string, Waiter> waiters;
        private CancellationTokenSource cts;
        private Task rpcReplyReceiverTask;

        public RemoteHub(Qlue.Logging.ILogFactory logFactory, string hostName, string ourDeviceId, string rpcQueueName = "gutter")
        {
            this.log = logFactory.GetLogger("RemoteHub");
            this.hostName = hostName;
            this.ourDeviceId = ourDeviceId;
            this.rpcQueueName = rpcQueueName;

            this.exchanges = new Dictionary<string, IModel>();
            this.waiters = new Dictionary<string, Waiter>();

            this.factory = new ConnectionFactory
            {
                HostName = this.hostName,
                AutomaticRecoveryEnabled = true,
                RequestedConnectionTimeout = 4000
            };

            this.connection = factory.CreateConnection();

            this.serializer = new Serializer();

            this.rpcModel = this.connection.CreateModel();
            this.rpcReplyQueue = this.rpcModel.QueueDeclare();

            this.cts = new CancellationTokenSource();
            this.rpcReplyReceiverTask = Task.Run(() =>
            {
                InternalReceiverRPCReply(cts.Token);
            }, cts.Token);
        }

        private IModel GetFanoutModel(string exchangeName)
        {
            IModel model;
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

        public void Dispose()
        {
            this.cts.Cancel();
            this.rpcReplyReceiverTask.Wait();

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

            if (this.rpcModel != null)
            {
                this.rpcModel.Close();
                this.rpcModel.Dispose();

                this.rpcModel = null;
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

        public Payload.IPayload SendRpc(Payload.IPayload payload)
        {
            return SendRpc(payload, TimeSpan.Zero);
        }

        public T SendRpc<T>(Payload.IPayload payload, TimeSpan timeout) where T : class
        {
            return SendRpc(payload, timeout) as T;
        }

        public Payload.IPayload SendRpc(Payload.IPayload payload, TimeSpan timeout)
        {
            if (timeout == TimeSpan.Zero)
            {
                // Default timeout is 10 seconds
                timeout = TimeSpan.FromSeconds(10);
            }

            IBasicProperties properties;
            var busPayload = GenerateBusMessage(payload, out properties);

            string message = this.serializer.SerializeObject(busPayload);

            var body = Encoding.UTF8.GetBytes(message);

            properties.ReplyTo = this.rpcReplyQueue;

            try
            {
                var waiter = new Waiter();
                lock (this.waiters)
                {
                    this.waiters.Add(properties.MessageId, waiter);
                }

                var watch = System.Diagnostics.Stopwatch.StartNew();

                this.rpcModel.BasicPublish(string.Empty, this.rpcQueueName, properties, body);

#if VERBOSE_LOGGING
                this.log.Trace("Sent {0} bytes", body.Length);
#endif

                // Wait for response
                var response = waiter.GetResponse(timeout);

                watch.Stop();

                this.log.Debug("RPC Duration {0:N0} ms", watch.ElapsedMilliseconds);

                return response;
            }
            finally
            {
                lock (this.waiters)
                {
                    this.waiters.Remove(properties.MessageId);
                }
            }
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

        public void ReceiverRPC(CancellationToken cancelToken, Func<Payload.RPCPayload, Payload.IPayload> func)
        {
            var channel = this.connection.CreateModel();

            channel.QueueDeclare(this.rpcQueueName, false, false, false, null);
            channel.BasicQos(0, 1, false);

            var consumer = new QueueingBasicConsumer(channel);
            channel.BasicConsume(this.rpcQueueName, false, consumer);

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

                        var busPayload = this.serializer.DeserializeString(Encoding.UTF8.GetString(body)) as Payload.BusPayload;

                        if (busPayload != null)
                        {
                            var rpcPayload = new Payload.RPCPayload
                            {
                                OriginDeviceId = busPayload.OriginDeviceId,
                                Request = busPayload.Payload
                            };

                            Observable.Start<Payload.IPayload>(() => func(rpcPayload), TaskPoolScheduler.Default)
                                .Subscribe(response =>
                                {
                                    if (response != null)
                                    {
                                        // Send response
                                        var replyProps = channel.CreateBasicProperties();
                                        replyProps.CorrelationId = result.BasicProperties.MessageId;

                                        byte[] responseBody = Encoding.UTF8.GetBytes(this.serializer.SerializeObject(response));

                                        channel.BasicPublish(string.Empty, result.BasicProperties.ReplyTo, replyProps, responseBody);
                                        channel.BasicAck(result.DeliveryTag, false);
                                    }
                                });
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

        private void InternalReceiverRPCReply(CancellationToken cancelToken)
        {
            using (var channel = this.connection.CreateModel())
            {
                var consumer = new QueueingBasicConsumer(channel);
                channel.BasicConsume(this.rpcReplyQueue, true, consumer);

                this.log.Trace("Waiting for messages");
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

                            Waiter waiter;
                            if (!this.waiters.TryGetValue(result.BasicProperties.CorrelationId, out waiter))
                            {
                                this.log.Debug("Nobody waited for correlation id {0}", result.BasicProperties.CorrelationId);
                                continue;
                            }

                            waiter.ReplyReceived(payload);
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
}
