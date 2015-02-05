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
    public class StormHub : IDisposable, IHub, IPlugin
    {
        private ILog log;
        private IUnityContainer container;
        private CancellationTokenSource cts;
        private RemoteHub remoteHub;
        private Task amqpReceivingTask;
        private Task amqpReceivingTaskRpc;
        private ISubject<Payload.InternalMessage> localQueue;
        private IObservable<Tuple<Payload.IPayload, InvokeContext>> externalIncomingQueue;
        private IObserver<Payload.InternalMessage> broadcastQueue;
        protected Dictionary<string, PluginInstance> pluginInstances;
        private List<IPlugin> runningInstances;
        private List<INode> runningNodes;
        private Config.HubConfig hubConfig;
        private Plugin.PluginManager pluginManager;
        private Config.RootZoneConfig rootZoneConfig;
        private string configPath;

        public StormHub(Config.HubConfig hubConfig, Plugin.PluginManager pluginManager, Config.RootZoneConfig rootZoneConfig, IUnityContainer container, string configPath)
        {
            this.hubConfig = hubConfig;
            this.pluginManager = pluginManager;
            this.container = container;
            this.rootZoneConfig = rootZoneConfig;
            this.configPath = configPath;

            this.log = container.Resolve<ILogFactory>().GetLogger("StormHub");

            this.runningInstances = new List<IPlugin>();
            this.runningNodes = new List<INode>();
            this.localQueue = new Subject<Payload.InternalMessage>();
            var externalIncomingSubject = new Subject<Tuple<Payload.IPayload, InvokeContext>>();
            this.externalIncomingQueue = externalIncomingSubject.AsObservable();

            if (!string.IsNullOrEmpty(this.hubConfig.UpstreamHub))
            {
                this.cts = new CancellationTokenSource();
                this.remoteHub = new RemoteHub(container.Resolve<ILogFactory>(), this.hubConfig.UpstreamHub, this.hubConfig.DeviceId);

                this.amqpReceivingTask = Task.Run(() =>
                {
                    this.remoteHub.Receiver(cts.Token, externalIncomingSubject.AsObserver());
                }, cts.Token);

                this.amqpReceivingTaskRpc = Task.Run(() =>
                {
                    this.remoteHub.ReceiverRPC(cts.Token, externalIncomingSubject.AsObserver());
                }, cts.Token);

                this.externalIncomingQueue.Subscribe(p =>
                {
                    this.log.Debug("Received external payload {0} ({1})", p.Item2.OriginDeviceId, p.Item1.GetDebugInfo());
                });
            }

            this.broadcastQueue = Observer.Create<Payload.InternalMessage>(p =>
                {
                    this.log.Debug("Received local payload {0}", p.Payload.GetDebugInfo());

                    // Send locally
                    this.localQueue.OnNext(p);

                    if (!string.IsNullOrEmpty(this.hubConfig.UpstreamHub) &&
                        !string.IsNullOrEmpty(p.DestinationZoneId) &&
                        p.Payload is Payload.IRemotePayload)
                    {
                        // Broadcast to remote hub
                        try
                        {
                            this.remoteHub.SendPayload(p.Payload);
                        }
                        catch (Exception ex)
                        {
                            this.log.WarnException(ex, "Failed to send message on Amqp queue");
                        }
                    }
                });

            this.pluginInstances = new Dictionary<string, PluginInstance>();

            // Wire-up the storm hub itself
            WireUpPlugin(this, this.externalIncomingQueue, this.localQueue.AsObservable());

            // Load plugins
            this.pluginManager.LoadPlugins(this, this.hubConfig.DeviceId, this.hubConfig.Plugins);
        }

        internal Tuple<PluginInstance, IPlugin> AddPluginInstance(string pluginId, string name, string instanceId, string zoneId)
        {
            if (this.pluginInstances.ContainsKey(instanceId))
                throw new ArgumentException("Duplicate InstanceId");

            var pluginInstance = new PluginInstance(pluginId, instanceId)
            {
                Name = name
            };

            lock (this.pluginInstances)
            {
                this.pluginInstances.Add(pluginInstance.InstanceId, pluginInstance);
            }

            var plugin = StartPluginInstance(pluginInstance);

            return Tuple.Create(pluginInstance, plugin);
        }

        public Tuple<PluginInstance, IPlugin> AddPluginInstance<T>(string name, string instanceId, string zoneId) where T : IPlugin
        {
            string pluginId = typeof(T).FullName;

            if (this.pluginInstances.ContainsKey(instanceId))
                throw new ArgumentException("Duplicate InstanceId");

            var pluginInstance = new PluginInstance(pluginId, instanceId)
            {
                Name = name
            };

            lock (this.pluginInstances)
            {
                this.pluginInstances.Add(pluginInstance.InstanceId, pluginInstance);
            }

            var plugin = LoadPlugin(pluginInstance, typeof(T));

            return Tuple.Create(pluginInstance, plugin);
        }

        public Tuple<PluginInstance, IPlugin> AddPluginInstance(AvailablePlugin plugin, string name, string instanceId, string zoneId)
        {
            return AddPluginInstance(plugin.PluginId, name, instanceId, zoneId);
        }

        public string InstanceId
        {
            get { return this.hubConfig.DeviceId; }
        }

        private IPlugin StartPluginInstance(PluginInstance pluginInstance)
        {
            try
            {
                var pluginType = this.pluginManager.GetPluginTypeFromPluginId(pluginInstance.PluginId);

                if (pluginType == null)
                {
                    this.log.Error("Plugin {0} for instance {1} not found", pluginInstance.PluginId, pluginInstance.Name);
                    return null;
                }

                var pluginInstanceType = this.pluginManager.LoadPluginType(pluginType);

                if (pluginInstanceType == null)
                {
                    this.log.Error("Failed to instantiate plugin {0}", pluginType.AssemblyQualifiedName);
                    return null;
                }

                var plugin = LoadPlugin(pluginInstance, pluginInstanceType);

                return plugin;
            }
            catch (Exception ex)
            {
                this.log.Warn("Failed to load plugin for device instance {0}, error: {1}, msg: {2}",
                    pluginInstance.Name, ex.GetType().Name, ex.Message);
                return null;
            }
        }

        private void WireUpPlugin(
            IPlugin plugin,
            IObservable<Tuple<Payload.IPayload, InvokeContext>> externalIncoming,
            IObservable<Payload.InternalMessage> internalIncoming)
        {
            var methods = plugin.GetType().GetMethods().Where(x => x.Name == "Incoming");

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();

                if (parameters.Length < 1)
                {
                    // Unsupported signature
                    continue;
                }

                var payloadType = parameters.First().ParameterType;

                if (!typeof(Payload.IPayload).IsAssignableFrom(payloadType))
                    continue;

                if (parameters.Length > 1 && parameters[1].ParameterType != typeof(InvokeContext))
                    continue;

                externalIncoming
                    .Subscribe(x => InvokeIncoming(x.Item1, x.Item2, method, parameters, plugin));

                // Filter out our own messages
                internalIncoming
                    .Where(x => x.OriginatingInstanceId != plugin.InstanceId &&
                        (string.IsNullOrEmpty(x.DestinationInstanceId) || x.DestinationInstanceId == plugin.InstanceId))
                    .Subscribe(x => InvokeIncoming(x, method, parameters, plugin));
            }
        }

        private void WireUpNode(
            INode node,
            IObservable<Tuple<Payload.IPayload, InvokeContext>> externalIncoming,
            IObservable<Payload.InternalMessage> internalIncoming)
        {
            var methods = node.GetType().GetMethods().Where(x => x.Name == "Incoming");

            foreach (var method in methods)
            {
                var parameters = method.GetParameters();

                if (parameters.Length < 1)
                {
                    // Unsupported signature
                    continue;
                }

                var payloadType = parameters.First().ParameterType;

                if (!typeof(Payload.IPayload).IsAssignableFrom(payloadType))
                    continue;

                if (parameters.Length > 1 && parameters[1].ParameterType != typeof(InvokeContext))
                    continue;

                externalIncoming
                    .Subscribe(x => InvokeIncoming(x.Item1, x.Item2, method, parameters, node));

                // Filter out our own messages
                internalIncoming
                    .Where(x => x.OriginatingInstanceId != node.InstanceId &&
                        (string.IsNullOrEmpty(x.DestinationInstanceId) || x.DestinationInstanceId == node.InstanceId))
                    .Subscribe(x => InvokeIncoming(x, method, parameters, node));
            }
        }

        private void InvokeIncoming(Payload.IPayload payload, InvokeContext invCtx, MethodInfo method, ParameterInfo[] parameters, object instance)
        {
            try
            {
                var payloadType = parameters.First().ParameterType;

                if (payloadType.IsInstanceOfType(payload))
                {
                    if (parameters.Length > 1 && invCtx != null)
                        method.Invoke(instance, new object[] { payload, invCtx });
                    else
                        method.Invoke(instance, new object[] { payload });
                }
            }
            catch (Exception ex)
            {
                this.log.WarnException("Exception when invoking Incoming method", ex);
            }
        }

        private void InvokeIncoming(Payload.InternalMessage intMessage, MethodInfo method, ParameterInfo[] parameters, object instance)
        {
            try
            {
                var payloadType = parameters.First().ParameterType;

                if (payloadType.IsInstanceOfType(intMessage.Payload))
                {
                    if (parameters.Length > 1)
                    {
                        var invCtx = new InvokeContext(intMessage);

                        method.Invoke(instance, new object[] { intMessage.Payload, invCtx });
                    }
                    else
                        method.Invoke(instance, new object[] { intMessage.Payload });
                }
            }
            catch (Exception ex)
            {
                this.log.WarnException("Exception when invoking Incoming method", ex);
            }
        }

        [Obsolete]
        public T LoadPlugin<T>(PluginInstance pluginInstance /*params ParameterOverride[] overrides*/) where T : IPlugin
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", Guid.NewGuid().ToString("n")));

            var plugin = this.container.Resolve<T>(allOverrides.ToArray());

            WireUpPlugin(plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

            this.runningInstances.Add(plugin);

            return plugin;
        }

        public IPlugin LoadPlugin(PluginInstance pluginInstance, Type type, params ParameterOverride[] overrides)
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", pluginInstance.InstanceId));
            allOverrides.AddRange(overrides);

            IPlugin plugin;
            try
            {
                plugin = this.container.Resolve(type, allOverrides.ToArray()) as IPlugin;
            }
            catch (Microsoft.Practices.Unity.ResolutionFailedException ex)
            {
                if (ex.InnerException != null)
                    // Unwrap
                    throw ex.InnerException;
                throw;
            }

            WireUpPlugin(plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

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
                    this.amqpReceivingTaskRpc.Wait();
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

                this.runningInstances = null;
            }

            if (this.runningNodes != null)
            {
                foreach (var nodeInstance in this.runningNodes)
                {
                    var disposable = nodeInstance as IDisposable;

                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                this.runningNodes = null;
            }
        }

        public void AddNode(INode nodeInstance)
        {
            WireUpNode(nodeInstance, this.externalIncomingQueue, this.localQueue.AsObservable());
            this.runningNodes.Add(nodeInstance);
        }

        public void BroadcastPayload(IPlugin sender, Payload.IPayload payload, string destinationZoneId, string originatingZoneId)
        {
            PluginInstance instance;
            if (!this.pluginInstances.TryGetValue(sender.InstanceId, out instance))
                throw new ArgumentException("Unknown/invalid sender (missing InstanceId)");

            this.broadcastQueue.OnNext(new Payload.InternalMessage(
                originatingInstanceId: instance.InstanceId,
                destinationInstanceId: null,
                payload: payload,
                originatingZoneId: originatingZoneId,
                destinationZoneId: destinationZoneId));
        }

        public void SendPayload(string senderInstanceId, string destinationInstanceId, Payload.IPayload payload)
        {
            // No zone attached
            this.broadcastQueue.OnNext(new Payload.InternalMessage(
                originatingInstanceId: senderInstanceId,
                payload: payload,
                destinationInstanceId: destinationInstanceId,
                originatingZoneId: null,
                destinationZoneId: null));
        }

        public string GetSetting(IPlugin device, string key, string defaultValue)
        {
            var pluginConfig = this.hubConfig.GetPluginConfig(device.GetType().FullName, device.InstanceId);

            return pluginConfig.GetSetting(key, defaultValue);
        }

        public Payload.IPayload Rpc(Payload.IPayload request)
        {
            if (this.remoteHub == null)
                throw new InvalidOperationException("Remote hub not configured");

            return this.remoteHub.SendRpc(request, TimeSpan.FromSeconds(10));
        }

        private List<Payload.Management.Zone> GetZones(List<Config.ZoneConfig> zoneConfigs)
        {
            var list = new List<Payload.Management.Zone>();

            foreach (var zoneConfig in zoneConfigs)
            {
                var newZone = new Payload.Management.Zone
                {
                    Name = zoneConfig.Name,
                    ZoneId = zoneConfig.ZoneId
                };

                if (zoneConfig.Zones.Any())
                    newZone.Zones = GetZones(zoneConfig.Zones);

                list.Add(newZone);
            }

            return list;
        }

        public void Incoming(Payload.Management.ListZonesRequest request, InvokeContext invCtx)
        {
            var zoneResponse = new Payload.Management.ListZonesResponse
            {
                Zones = GetZones(this.rootZoneConfig.Zones)
            };

            invCtx.Response.OnNext(zoneResponse);
        }

        public string ConfigPath
        {
            get { return this.configPath; }
        }
    }
}
