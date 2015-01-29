using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RabbitMQ.Client;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Practices.Unity;
using Qlue.Logging;
using System.Reflection;
using Newtonsoft.Json;

namespace IoStorm
{
    //TODO: At some point we should probably split this into a StormHub component that handles communication/IPC
    //TODO:   and a PluginManager
    public class StormHub : IDisposable, IHub
    {
        private ILog log;
        private IUnityContainer container;
        private CancellationTokenSource cts;
        private RemoteHub remoteHub;
        private Task amqpReceivingTask;
        private ISubject<Payload.InternalMessage> localQueue;
        private IObservable<Payload.BusPayload> externalIncomingQueue;
        private IObserver<Payload.InternalMessage> broadcastQueue;
        protected Dictionary<string, PluginInstance> deviceInstances;
        private List<IPlugin> runningInstances;
        private Config.HubConfig hubConfig;
        private Plugin.PluginManager pluginManager;

        public StormHub(Config.HubConfig hubConfig, Plugin.PluginManager pluginManager, IUnityContainer container)
        {
            this.hubConfig = hubConfig;
            this.pluginManager = pluginManager;
            this.container = container;

            this.log = container.Resolve<ILogFactory>().GetLogger("StormHub");

            this.runningInstances = new List<IPlugin>();
            this.localQueue = new Subject<Payload.InternalMessage>();
            var externalIncomingSubject = new Subject<Payload.BusPayload>();
            this.externalIncomingQueue = externalIncomingSubject.AsObservable();

            if (!string.IsNullOrEmpty(this.hubConfig.UpstreamHub))
            {
                this.cts = new CancellationTokenSource();
                this.remoteHub = new RemoteHub(container.Resolve<ILogFactory>(), this.hubConfig.UpstreamHub, this.hubConfig.DeviceId);

                this.amqpReceivingTask = Task.Run(() =>
                {
                    this.remoteHub.Receiver("Global", cts.Token, externalIncomingSubject.AsObserver());
                }, cts.Token);

                this.externalIncomingQueue.Subscribe(p =>
                {
                    this.log.Debug("Received external payload {0} ({1})", p.OriginDeviceId, p.Payload.GetDebugInfo());
                });
            }

            this.broadcastQueue = Observer.Create<Payload.InternalMessage>(p =>
                {
                    this.log.Debug("Received local payload {0}", p.Payload.GetDebugInfo());

                    // Send locally
                    this.localQueue.OnNext(p);

                    if (!string.IsNullOrEmpty(this.hubConfig.UpstreamHub))
                    {
                        // Broadcast on amqp
                        try
                        {
                            if (p.Payload is Payload.IRemotePayload)
                                this.remoteHub.SendPayload("Global", p.Payload);
                        }
                        catch (Exception ex)
                        {
                            this.log.WarnException(ex, "Failed to send message on Amqp queue");
                        }
                    }
                });

            this.deviceInstances = new Dictionary<string, PluginInstance>();

            // Load plugins
            this.pluginManager.LoadPlugins(this, this.hubConfig.DeviceId, this.hubConfig.Plugins);
        }

        internal Tuple<PluginInstance, IPlugin> AddDeviceInstance(string pluginId, string name, string instanceId, string zoneId)
        {
            if (this.deviceInstances.ContainsKey(instanceId))
                throw new ArgumentException("Duplicate InstanceId");

            var pluginInstance = new PluginInstance(pluginId, instanceId)
            {
                ZoneId = zoneId,
                Name = name
            };

            lock (this.deviceInstances)
            {
                this.deviceInstances.Add(pluginInstance.InstanceId, pluginInstance);
            }

            var plugin = StartPluginInstance(pluginInstance);

            var pluginConfig = this.hubConfig.GetPluginConfig(pluginInstance.PluginId, pluginInstance.InstanceId);

            return Tuple.Create(pluginInstance, plugin);
        }

        public Tuple<PluginInstance, IPlugin> AddDeviceInstance<T>(string name, string instanceId, string zoneId) where T : IPlugin
        {
            string pluginId = typeof(T).FullName;

            if (this.deviceInstances.ContainsKey(instanceId))
                throw new ArgumentException("Duplicate InstanceId");

            var deviceInstance = new PluginInstance(pluginId, instanceId)
            {
                ZoneId = zoneId,
                Name = name
            };

            lock (this.deviceInstances)
            {
                this.deviceInstances.Add(deviceInstance.InstanceId, deviceInstance);
            }

            var pluginInstance = LoadPlugin(deviceInstance, typeof(T));

            //FIXME: Re-save settings?

            return Tuple.Create(deviceInstance, pluginInstance);
        }

        public Tuple<PluginInstance, IPlugin> AddDeviceInstance(AvailablePlugin plugin, string name, string instanceId, string zoneId)
        {
            return AddDeviceInstance(plugin.PluginId, name, instanceId, zoneId);
        }

        [Obsolete]
        public Tuple<PluginInstance, IPlugin> AddDeviceInstance(AvailablePlugin plugin, string name)
        {
            return AddDeviceInstance(plugin, name, Guid.NewGuid().ToString("N"), this.hubConfig.DeviceId);
        }

        private IPlugin StartPluginInstance(PluginInstance deviceInstance)
        {
            try
            {
                var pluginType = this.pluginManager.GetPluginTypeFromPluginId(deviceInstance.PluginId);

                if (pluginType == null)
                {
                    this.log.Error("Plugin {0} for instance {1} not found", deviceInstance.PluginId, deviceInstance.Name);
                    return null;
                }

                var pluginInstanceType = this.pluginManager.LoadPluginType(pluginType);

                if (pluginInstanceType == null)
                {
                    this.log.Error("Failed to instantiate plugin {0}", pluginType.AssemblyQualifiedName);
                    return null;
                }

                var pluginInstance = LoadPlugin(deviceInstance, pluginInstanceType);

                return pluginInstance;
            }
            catch (Exception ex)
            {
                this.log.WarnException(ex, "Failed to load plugin for device instance {0}", deviceInstance.Name);
                return null;
            }
        }

        private void WireUpPlugin(
            PluginInstance deviceInstance,
            IPlugin plugin,
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

                externalIncoming
                    .Subscribe(x =>
                    {
                        try
                        {
                            // Unwrap
                            Payload.IPayload unwrappedPayload = UnwrapPayload(x.Payload, deviceInstance.ZoneId);

                            if (unwrappedPayload != null && parameterType.IsInstanceOfType(unwrappedPayload))
                                method.Invoke(plugin, new object[] { unwrappedPayload });
                        }
                        catch (Exception ex)
                        {
                            this.log.WarnException("Exception when invoking Incoming method", ex);
                        }
                    });

                // Filter out our own messages
                internalIncoming
                    .Where(x => x.OriginatingInstanceId != plugin.InstanceId)
                    .Subscribe(x =>
                    {
                        try
                        {
                            // Unwrap
                            Payload.IPayload unwrappedPayload = UnwrapPayload(x.Payload, deviceInstance.ZoneId);

                            if (unwrappedPayload != null && parameterType.IsInstanceOfType(unwrappedPayload))
                                method.Invoke(plugin, new object[] { unwrappedPayload });
                        }
                        catch (Exception ex)
                        {
                            this.log.WarnException("Exception when invoking Incoming method", ex);
                        }
                    });
            }
        }

        private Payload.IPayload UnwrapPayload(Payload.IPayload incoming, string zoneId)
        {
            string sourceZoneId = null;
            var zoneSourcePayload = incoming as Payload.ZoneSourcePayload;
            if (zoneSourcePayload != null)
            {
                sourceZoneId = zoneSourcePayload.SourceZoneId;
                incoming = zoneSourcePayload.Payload;
            }

            var zoneDestinationPayload = incoming as Payload.ZoneDestinationPayload;
            if (zoneDestinationPayload != null)
            {
                if (string.Equals(zoneDestinationPayload.DestinationZoneId, zoneId))
                    return zoneDestinationPayload.Payload;
            }

            return incoming;
        }

        [Obsolete]
        public T LoadPlugin<T>(PluginInstance deviceInstance /*params ParameterOverride[] overrides*/) where T : IPlugin
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", Guid.NewGuid().ToString("n")));

            var plugin = this.container.Resolve<T>(allOverrides.ToArray());

            WireUpPlugin(deviceInstance, plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

            this.runningInstances.Add(plugin);

            return plugin;
        }

        public IPlugin LoadPlugin(PluginInstance deviceInstance, Type type, params ParameterOverride[] overrides)
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", deviceInstance.InstanceId));
            allOverrides.AddRange(overrides);

            var plugin = this.container.Resolve(type, allOverrides.ToArray()) as IPlugin;

            WireUpPlugin(deviceInstance, plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

            this.runningInstances.Add(plugin);

            return plugin;
        }

        public void Dispose()
        {
            if (this.cts != null)
            {
                this.cts.Cancel();

                try
                {
                    this.amqpReceivingTask.Wait();
                }
                catch
                {
                }

                this.cts = null;
            }

            if (this.remoteHub != null)
            {
                this.remoteHub.Dispose();
                this.remoteHub = null;
            }

            if (this.runningInstances != null)
            {
                foreach (var pluginInstance in this.runningInstances)
                {
                    var disposable = pluginInstance as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                this.deviceInstances = null;
            }
        }

        public void BroadcastPayload(IPlugin sender, Payload.IPayload payload)
        {
            PluginInstance instance;
            if (!this.deviceInstances.TryGetValue(sender.InstanceId, out instance))
                throw new ArgumentException("Unknown/invalid sender (missing InstanceId)");

            var zonePayload = new IoStorm.Payload.ZoneSourcePayload
            {
                SourceZoneId = instance.ZoneId,
                Payload = payload
            };

            this.broadcastQueue.OnNext(new Payload.InternalMessage(instance.InstanceId, zonePayload));
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

        public string GetSetting(IPlugin device, string key, string defaultValue)
        {
            var pluginConfig = this.hubConfig.GetPluginConfig(device.GetType().FullName, device.InstanceId);

            return pluginConfig.GetSetting(key, defaultValue);
        }

        [Obsolete("Not right")]
        public string ZoneId
        {
            get { return this.hubConfig.DeviceId; }
        }
    }
}
