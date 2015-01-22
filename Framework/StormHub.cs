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
        private PluginManager<IDevice> pluginManager;
        protected Dictionary<string, DeviceInstance> deviceInstances;
        private IReadOnlyList<AvailablePlugin> availablePlugins;
        private List<IDevice> runningInstances;

        public StormHub(IUnityContainer container, string ourDeviceId, string remoteHubHost = null)
        {
            this.container = container;
            this.ourDeviceId = ourDeviceId;

            this.log = container.Resolve<ILogFactory>().GetLogger("StormHub");

            string pluginFullPath = GetFullPath("Plugins");
            if (!Directory.Exists(pluginFullPath))
                Directory.CreateDirectory(pluginFullPath);

            this.configPath = GetFullPath("Config");
            if (!Directory.Exists(this.configPath))
                Directory.CreateDirectory(this.configPath);

            // Copy common dependencies
            File.Copy("IoStorm.CorePayload.dll", Path.Combine(pluginFullPath, "IoStorm.CorePayload.dll"), true);
            File.Copy("IoStorm.Framework.dll", Path.Combine(pluginFullPath, "IoStorm.Framework.dll"), true);
            this.pluginManager = new PluginManager<IDevice>(pluginFullPath,
                "IoStorm.CorePayload.dll",
                "IoStorm.Framework.dll");

            this.availablePlugins = this.pluginManager.GetAvailablePlugins();
            this.runningInstances = new List<IDevice>();
            this.localQueue = new Subject<Payload.InternalMessage>();
            var externalIncomingSubject = new Subject<Payload.BusPayload>();
            this.externalIncomingQueue = externalIncomingSubject.AsObservable();

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

            //try
            //{
            //    this.deviceInstances = BinaryRage.DB.Get<List<DeviceInstance>>("DeviceInstances", this.configPath);
            //}
            //catch (DirectoryNotFoundException)
            //{
            //    this.deviceInstances = new List<DeviceInstance>();
            //}

            //StartDeviceInstances();
        }

        public IReadOnlyList<AvailablePlugin> AvailablePlugins
        {
            get { return this.availablePlugins; }
        }

        public IReadOnlyList<DeviceInstance> DeviceInstances
        {
            get { return this.deviceInstances.Values.ToList().AsReadOnly(); }
        }

        internal Tuple<DeviceInstance, IDevice> AddDeviceInstance(string pluginId, string name, string instanceId, string zoneId, IDictionary<string, string> settings)
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

            SaveDeviceInstances();

            // Save settings
            foreach (var setting in settings)
            {
                SaveSetting(deviceInstance.PluginId, deviceInstance.InstanceId, setting.Key, setting.Value);
            }

            var pluginInstance = StartDeviceInstance(deviceInstance);

            return Tuple.Create(deviceInstance, pluginInstance);
        }

        public Tuple<DeviceInstance, IDevice> AddDeviceInstance<T>(string name, string instanceId, string zoneId, IDictionary<string, string> settings) where T : IDevice
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

            SaveDeviceInstances();

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

        public Tuple<DeviceInstance, IDevice> AddDeviceInstance(AvailablePlugin plugin, string name, string instanceId, string zoneId, IDictionary<string, string> settings)
        {
            return AddDeviceInstance(plugin.PluginId, name, instanceId, zoneId, settings);
        }

        [Obsolete]
        public Tuple<DeviceInstance, IDevice> AddDeviceInstance(AvailablePlugin plugin, string name, params Tuple<string, string>[] settings)
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

        private IDevice StartDeviceInstance(DeviceInstance deviceInstance)
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

        public void SaveDeviceInstances()
        {
            //TODO Not sure about this one
            BinaryRage.DB.Insert("DeviceInstances", this.deviceInstances, this.configPath);
        }

        public string GetFullPath(string pluginRelativePath)
        {
            string assemblyLoc = Assembly.GetExecutingAssembly().Location;
            string currentDirectory = assemblyLoc.Substring(0, assemblyLoc.LastIndexOf(Path.DirectorySeparatorChar) + 1);
            string fullPath = Path.Combine(currentDirectory, pluginRelativePath);

            return fullPath;
        }

        private void WireUpPlugin(
            DeviceInstance deviceInstance,
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
        public T LoadPlugin<T>(DeviceInstance deviceInstance /*params ParameterOverride[] overrides*/) where T : IDevice
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", Guid.NewGuid().ToString("n")));

            var plugin = this.container.Resolve<T>(allOverrides.ToArray());

            WireUpPlugin(deviceInstance, plugin, this.externalIncomingQueue, this.localQueue.AsObservable());

            this.runningInstances.Add(plugin);

            return plugin;
        }

        public IDevice LoadPlugin(DeviceInstance deviceInstance, Type type, params ParameterOverride[] overrides)
        {
            var allOverrides = new List<ResolverOverride>();
            allOverrides.Add(new DependencyOverride<IHub>(this));
            allOverrides.Add(new ParameterOverride("instanceId", deviceInstance.InstanceId));
            allOverrides.AddRange(overrides);

            var plugin = this.container.Resolve(type, allOverrides.ToArray()) as IDevice;

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

            BinaryRage.DB.WaitForCompletion();
        }

        public void BroadcastPayload(IDevice sender, Payload.IPayload payload)
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

        public string GetSetting(IDevice device, string key)
        {
            string fullKey = string.Format("{0}-{1}-{2}", device.GetType().FullName, device.InstanceId, key);

            try
            {
                return BinaryRage.DB.Get<string>(fullKey, this.configPath);
            }
            catch (DirectoryNotFoundException)
            {
                string value = null;

                BinaryRage.DB.Insert(fullKey, value, this.configPath);

                return value;
            }
        }

        public void SaveSetting(string deviceName, string instanceId, string key, string value)
        {
            string fullKey = string.Format("{0}-{1}-{2}", deviceName, instanceId, key);

            BinaryRage.DB.Insert(fullKey, value, this.configPath);
        }

        public string ZoneId
        {
            get { return this.ourDeviceId; }
        }
    }
}
