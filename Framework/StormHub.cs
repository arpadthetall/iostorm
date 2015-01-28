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
        private string configPath;
        private PluginManager<IPlugin> pluginManager;
        protected Dictionary<string, DeviceInstance> deviceInstances;
        private IReadOnlyList<AvailablePlugin> availablePlugins;
        private List<IPlugin> runningInstances;
        private Dictionary<string, PluginConfig.InstanceSettings> pluginSettings;

        public StormHub(IUnityContainer container, string ourDeviceId, string configPath, string pluginPath, string remoteHubHost = null)
        {
            var assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            this.container = container;
            this.ourDeviceId = ourDeviceId;
            this.configPath = configPath;

            this.log = container.Resolve<ILogFactory>().GetLogger("StormHub");

            // Copy common dependencies
            File.Copy(Path.Combine(assemblyPath, "IoStorm.CorePayload.dll"), Path.Combine(pluginPath, "IoStorm.CorePayload.dll"), true);
            File.Copy(Path.Combine(assemblyPath, "IoStorm.Framework.dll"), Path.Combine(pluginPath, "IoStorm.Framework.dll"), true);
            this.pluginManager = new PluginManager<IPlugin>(pluginPath,
                "IoStorm.CorePayload.dll",
                "IoStorm.Framework.dll");

            this.availablePlugins = this.pluginManager.GetAvailablePlugins();
            this.runningInstances = new List<IPlugin>();
            this.localQueue = new Subject<Payload.InternalMessage>();
            var externalIncomingSubject = new Subject<Payload.BusPayload>();
            this.externalIncomingQueue = externalIncomingSubject.AsObservable();
            this.pluginSettings = new Dictionary<string, PluginConfig.InstanceSettings>();

            if (!string.IsNullOrEmpty(remoteHubHost))
            {
                this.cts = new CancellationTokenSource();
                this.remoteHub = new RemoteHub(container.Resolve<ILogFactory>(), remoteHubHost, ourDeviceId);

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

                    if (!string.IsNullOrEmpty(remoteHubHost))
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

            this.deviceInstances = new Dictionary<string, DeviceInstance>();
        }

        public IReadOnlyList<AvailablePlugin> AvailablePlugins
        {
            get { return this.availablePlugins; }
        }

        public IReadOnlyList<DeviceInstance> DeviceInstances
        {
            get { return this.deviceInstances.Values.ToList().AsReadOnly(); }
        }

        internal Tuple<DeviceInstance, IPlugin> AddDeviceInstance(string pluginId, string name, string instanceId, string zoneId, IDictionary<string, string> settings)
        {
            if (this.deviceInstances.ContainsKey(instanceId))
                throw new ArgumentException("Duplicate InstanceId");

            var deviceInstance = new DeviceInstance(pluginId, instanceId)
            {
                ZoneId = zoneId,
                Name = name
            };

            lock (this.deviceInstances)
            {
                this.deviceInstances.Add(deviceInstance.InstanceId, deviceInstance);
            }

            // Save settings
            foreach (var setting in settings)
            {
                SaveSetting(deviceInstance.PluginId, deviceInstance.InstanceId, setting.Key, setting.Value);
            }

            var pluginInstance = StartDeviceInstance(deviceInstance);

            return Tuple.Create(deviceInstance, pluginInstance);
        }

        public Tuple<DeviceInstance, IPlugin> AddDeviceInstance<T>(string name, string instanceId, string zoneId, IDictionary<string, string> settings) where T : IPlugin
        {
            string pluginId = typeof(T).FullName;

            if (this.deviceInstances.ContainsKey(instanceId))
                throw new ArgumentException("Duplicate InstanceId");

            var deviceInstance = new DeviceInstance(pluginId, instanceId)
            {
                ZoneId = zoneId,
                Name = name
            };

            lock (this.deviceInstances)
            {
                this.deviceInstances.Add(deviceInstance.InstanceId, deviceInstance);
            }

            // Save settings
            if (settings != null)
            {
                foreach (var setting in settings)
                {
                    SaveSetting(deviceInstance.PluginId, deviceInstance.InstanceId, setting.Key, setting.Value);
                }
            }

            var pluginInstance = LoadPlugin(deviceInstance, typeof(T));

            return Tuple.Create(deviceInstance, pluginInstance);
        }

        public Tuple<DeviceInstance, IPlugin> AddDeviceInstance(AvailablePlugin plugin, string name, string instanceId, string zoneId, IDictionary<string, string> settings)
        {
            return AddDeviceInstance(plugin.PluginId, name, instanceId, zoneId, settings);
        }

        [Obsolete]
        public Tuple<DeviceInstance, IPlugin> AddDeviceInstance(AvailablePlugin plugin, string name, params Tuple<string, string>[] settings)
        {
            return AddDeviceInstance(plugin, name, Guid.NewGuid().ToString("N"), this.ourDeviceId,
                settings.ToDictionary(k => k.Item1, v => v.Item2));
        }

        //private void StartDeviceInstances()
        //{
        //    foreach (var deviceInstance in this.deviceInstances.Values)
        //    {
        //        StartDeviceInstance(deviceInstance);
        //    }
        //}

        private IPlugin StartDeviceInstance(DeviceInstance deviceInstance)
        {
            try
            {
                var pluginType = this.pluginManager.GetAvailablePlugins().FirstOrDefault(x => x.PluginId == deviceInstance.PluginId);

                if (pluginType == null)
                {
                    this.log.Error("Plugin {0} for instance {1} not found", deviceInstance.PluginId, deviceInstance.Name);
                    return null;
                }

                var pluginInstanceType = this.pluginManager.LoadPluginType(pluginType.AssemblyQualifiedName);

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
            DeviceInstance deviceInstance,
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
        public T LoadPlugin<T>(DeviceInstance deviceInstance /*params ParameterOverride[] overrides*/) where T : IPlugin
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", Guid.NewGuid().ToString("n")));

            var plugin = this.container.Resolve<T>(allOverrides.ToArray());

            WireUpPlugin(deviceInstance, plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

            this.runningInstances.Add(plugin);

            return plugin;
        }

        public IPlugin LoadPlugin(DeviceInstance deviceInstance, Type type, params ParameterOverride[] overrides)
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
            DeviceInstance instance;
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

        public void SaveSettingsToFile(string pluginId, string instanceId)
        {
            if (Path.GetInvalidFileNameChars().Any(x => instanceId.Contains(x)))
                throw new InvalidDataException("InstanceId has to be a valid file name");

            PluginConfig.InstanceSettings instanceSettings = GetInstanceSettings(pluginId, instanceId);

            string configFileName = Path.Combine(this.configPath, "Plugin", instanceId + ".json");

            using (var file = File.CreateText(configFileName))
            {
                file.Write(instanceSettings.GetJson());
            }

            instanceSettings.ResetDirtyFlag();
        }

        private PluginConfig.InstanceSettings GetInstanceSettings(string pluginId, string instanceId)
        {
            PluginConfig.InstanceSettings instanceSettings;
            lock (this)
            {
                string key = pluginId + ":" + instanceId;
                if (this.pluginSettings.TryGetValue(key, out instanceSettings))
                {
                    instanceSettings = new PluginConfig.InstanceSettings(instanceId);

                    this.pluginSettings.Add(key, instanceSettings);
                }
            }

            return instanceSettings;
        }

        public string GetSetting(IPlugin device, string key)
        {
            var instanceSettings = GetInstanceSettings(device.GetType().Name, device.InstanceId);

            return instanceSettings.GetSetting(key, null);
        }

        private void SaveSetting(string pluginId, string instanceId, string key, string value)
        {
            var instanceSettings = GetInstanceSettings(pluginId, instanceId);

            if (instanceSettings.SetSetting(key, value))
                SaveSettingsToFile(pluginId, instanceId);
        }

        public void SaveSetting(IPlugin device, string key, string value)
        {
            SaveSetting(device.GetType().Name, device.InstanceId, key, value);
        }

        public string ZoneId
        {
            get { return this.ourDeviceId; }
        }
    }
}
