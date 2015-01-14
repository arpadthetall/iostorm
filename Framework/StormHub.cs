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

namespace IoStorm
{
    public class StormHub : IDisposable, IHub
    {
        private ILog log;
        private IUnityContainer container;
        private string ourDeviceId;
        private CancellationTokenSource cts;
        private RemoteHub remoteHub;
        private Task amqpReceivingTask;
        private ISubject<Payload.InternalMessage> localQueue;
        private IObservable<Payload.BusPayload> externalIncomingQueue;
        private IObserver<Payload.InternalMessage> broadcastQueue;

        public StormHub(IUnityContainer container, string ourDeviceId, string remoteHubHost = "localhost")
        {
            this.container = container;
            this.ourDeviceId = ourDeviceId;

            this.log = container.Resolve<ILogFactory>().GetLogger("StormHub");

            this.cts = new CancellationTokenSource();
            this.remoteHub = new RemoteHub(container.Resolve<ILogFactory>(), remoteHubHost, ourDeviceId);

            this.localQueue = new Subject<Payload.InternalMessage>();
            var externalIncomingSubject = new Subject<Payload.BusPayload>();

            this.broadcastQueue = Observer.Create<Payload.InternalMessage>(p =>
                {
                    this.log.Debug("Received local payload {0}", p.Payload.GetDebugInfo());

                    // Send locally
                    this.localQueue.OnNext(p);

                    // Broadcast on amqp
                    try
                    {
                        this.remoteHub.SendPayload("Global", p.Payload);
                    }
                    catch (Exception ex)
                    {
                        this.log.WarnException(ex, "Failed to send message on Amqp queue");
                    }
                });

            this.externalIncomingQueue = externalIncomingSubject.AsObservable();

            this.amqpReceivingTask = Task.Run(() =>
            {
                this.remoteHub.Receiver("Global", cts.Token, externalIncomingSubject.AsObserver());
            }, cts.Token);

            this.externalIncomingQueue.Subscribe(p =>
                {
                    this.log.Debug("Received external payload {0} ({1})", p.OriginDeviceId, p.Payload.GetDebugInfo());
                });
        }

        private void WireUpPlugin(
            IDevice plugin,
            IObservable<Payload.BusPayload> externalIncoming,
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

                externalIncoming.Where(x => parameterType.IsInstanceOfType(x.Payload))
                    .Subscribe(x =>
                    {
                        method.Invoke(plugin, new object[] { x.Payload });
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

                    try
                    {
                        this.amqpReceivingTask.Wait();
                    }
                    catch
                    {
                    }

                    cts = null;
                }

                if (this.remoteHub != null)
                {
                    this.remoteHub.Dispose();
                    this.remoteHub = null;
                }
            }
        }

        public void BroadcastPayload(IDevice sender, Payload.IPayload payload)
        {
            this.broadcastQueue.OnNext(new Payload.InternalMessage(sender.InstanceId, payload));
        }

        public void Incoming<T>(Action<T> action) where T : Payload.IPayload
        {
            this.externalIncomingQueue.Where(x => typeof(T).IsInstanceOfType(x.Payload)).Subscribe(bp =>
                {
                    action((T)bp.Payload);
                });

            this.localQueue.Where(x => typeof(T).IsInstanceOfType(x.Payload)).Subscribe(bp =>
                {
                    action((T)bp.Payload);
                });
        }
    }
}
