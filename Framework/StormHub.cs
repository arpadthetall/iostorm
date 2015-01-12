using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Practices.Unity;
using Qlue.Logging;

namespace Storm
{
    public class StormHub : IDisposable, IHub
    {
        private IUnityContainer container;
        private string ourDeviceId;
        private CancellationTokenSource cts;
        private Amqp amqp;
        private Task amqpReceivingTask;
        private ISubject<Payload.InternalMessage> localQueue;
        private IObservable<Payload.IPayload> externalIncomingQueue;
        private IObserver<Payload.InternalMessage> broadcastQueue;

        public StormHub(IUnityContainer container, string ourDeviceId, string hubServer = "localhost")
        {
            this.container = container;
            this.ourDeviceId = ourDeviceId;

            this.cts = new CancellationTokenSource();
            this.amqp = new Amqp(container.Resolve<ILogFactory>(), hubServer, ourDeviceId);

            this.localQueue = new Subject<Payload.InternalMessage>();
            var externalIncomingSubject = new Subject<Payload.IPayload>();

            this.broadcastQueue = Observer.Create<Payload.InternalMessage>(p =>
                {
                    // Broadcast on amqp
                    this.amqp.SendPayload("Global", p.Payload);

                    // Send locally
                    this.localQueue.OnNext(p);
                });

            this.amqpReceivingTask = Task.Run(() =>
            {
                this.amqp.Receiver("Global", cts.Token, externalIncomingSubject.AsObserver());
            }, cts.Token);

            //            this.localQueue = localSubject.AsObservable();
            this.externalIncomingQueue = externalIncomingSubject.AsObservable();

            //            this.container.RegisterInstance<IObserver<Payload.IPayload>>(outgoingQueue);
        }

        private void WireUpPlugin(
            IDevice plugin,
            IObservable<Payload.IPayload> externalIncoming,
            IObservable<Payload.InternalMessage> internalIncoming)
        {
            var methods = plugin.GetType().GetMethods().Where(x => x.Name == "Incoming");

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();

                if (parameters.Length != 1)
                {
                    // Unknown signature
                    continue;
                }

                var parameterType = parameters.First().ParameterType;

                externalIncoming.Where(x => parameterType.IsInstanceOfType(x))
                    .Subscribe(x =>
                    {
                        method.Invoke(plugin, new object[] { x });
                    });

                // Filter out our own messages
                internalIncoming
                    .Where(x => x.OriginatingInstanceId != plugin.InstanceId && parameterType.IsInstanceOfType(x.Payload))
                    .Subscribe(x =>
                    {
                        method.Invoke(plugin, new object[] { x.Payload });
                    });
            }
        }

        public T LoadPlugin<T>(params ParameterOverride[] overrides) where T : IDevice
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.AddRange(overrides);

            var plugin = this.container.Resolve<T>(allOverrides.ToArray());

            plugin.InstanceId = Guid.NewGuid().ToString("n");

            WireUpPlugin(plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

            return plugin;
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
                if (cts != null)
                {
                    cts.Cancel();

                    this.amqpReceivingTask.Wait();

                    cts = null;
                }

                if (this.amqp != null)
                {
                    this.amqp.Dispose();
                    this.amqp = null;
                }
            }
        }

        public void BroadcastPayload(IDevice sender, Payload.IPayload payload)
        {
            this.broadcastQueue.OnNext(new Payload.InternalMessage(sender.InstanceId, payload));
        }
    }
}
