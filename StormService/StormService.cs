using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IoStorm.StormService.Config;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;

namespace IoStorm.StormService
{
    public class StormService
    {
        private static IUnityContainer container;
        private static Qlue.Logging.ILog log;

        public void Start()
        {
            string currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string configPath = GetFullPath(ConfigurationManager.AppSettings["ConfigFilePath"]);
            string pluginPath = GetFullPath(ConfigurationManager.AppSettings["PluginFilePath"]);

            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);
            if (!Directory.Exists(pluginPath))
                Directory.CreateDirectory(pluginPath);

            container = new UnityContainer();

            var logFactory = new Qlue.Logging.NLogFactoryProvider();
            container.RegisterInstance<Qlue.Logging.ILogFactory>(logFactory);

            log = logFactory.GetLogger("Main");

            log.Info("Start up");

            AppDomain.CurrentDomain.AssemblyResolve += (sender, arg) =>
            {
                // Search plugins subfolder for plugins
                string[] parts = arg.Name.Split(',');
                if (parts.Length > 0)
                {
                    string pluginFolder = pluginPath;

                    string assemblyFileName = Path.Combine(pluginFolder, parts[0] + ".dll");
                    if (File.Exists(assemblyFileName))
                    {
                        return Assembly.LoadFile(assemblyFileName);
                    }
                }
                return null;
            };

            log.Info("Config file path {0}", configPath);
            log.Info("Plugin file path {0}", pluginPath);

            Config.HubConfig hubConfig = LoadHubConfig(configPath);
            log.Info("Hub Device Id {0}", hubConfig.DeviceId);

            if (!string.IsNullOrEmpty(hubConfig.UpstreamHub))
                log.Info("Connecting to remote hub at {0}", hubConfig.UpstreamHub);

            using (var hub = new IoStorm.StormHub(
                container: container,
                ourDeviceId: hubConfig.DeviceId,
                configPath: configPath,
                pluginPath: pluginPath,
                remoteHubHost: hubConfig.UpstreamHub))
            {
                //                var plugins = hub.AvailablePlugins;

                LoadPlugins(hub, hubConfig.DeviceId, hubConfig.Plugins);

                var activityController = hub.AddDeviceInstance<ActivityController>("Activity Controller",
                    InstanceId.GetInstanceId(), hubConfig.DeviceId, null);

                // Map remote controls
                //                    CorePlugins.RemoteMapping.IrManSony.MapRemoteControl(irMan);
                //                    CorePlugins.RemoteMapping.IrManSqueezebox.MapRemoteControl(irMan);

                //                    var xlat = hub.LoadPlugin<IoStorm.CorePlugins.RemoteMapping.ProtocolToPayload>();
                //                    xlat.MapSqueezeBoxRemote();


                //var sample = hub.LoadPlugin<Sample1>();
                //hub.Incoming<Payload.Navigation.Up>(x =>
                //{
                //    hub.BroadcastPayload(sample, new Payload.Light.On
                //    {
                //        LightId = "053"
                //    });
                //});

                //hub.Incoming<Payload.Navigation.Down>(x =>
                //{
                //    hub.BroadcastPayload(sample, new Payload.Light.Off
                //    {
                //        LightId = "053"
                //    });
                //});


                /*                if (!string.IsNullOrEmpty(arguments.UpbSerialPort))
                                    hub.LoadPlugin<IoStorm.CorePlugins.UpbPim>(new ParameterOverride("serialPortName", arguments.UpbSerialPort));

                                hub.LoadPlugin<IoStorm.CorePlugins.YamahaReceiver>();

                                if (!string.IsNullOrEmpty(arguments.IrManSerialPort))
                                {
                                    var irMan = hub.LoadPlugin<IoStorm.CorePlugins.IrmanReceiver>(new ParameterOverride("serialPortName", arguments.IrManSerialPort));

                                    // Map remote controls
                                    CorePlugins.RemoteMapping.IrManSony.MapRemoteControl(irMan);
                                    CorePlugins.RemoteMapping.IrManSqueezebox.MapRemoteControl(irMan);

                                    var xlat = hub.LoadPlugin<IoStorm.CorePlugins.RemoteMapping.ProtocolToPayload>();
                                    xlat.MapSqueezeBoxRemote();
                                }

                                if (!string.IsNullOrEmpty(arguments.AudioSwitcherSerialPort))
                                {
                                    hub.LoadPlugin<CorePlugins.SerialSwitcher>(new ParameterOverride("serialPortName", arguments.AudioSwitcherSerialPort));
                                }


                                //                hub.LoadPlugin<Storm.Sonos.Sonos>();

                                // Test
                                var sample = hub.LoadPlugin<Sample1>();
                                hub.BroadcastPayload(sample, new Payload.Audio.ChangeVolume { Steps = 1 });

                                hub.Incoming<Payload.Navigation.Up>(x =>
                                {
                                    hub.BroadcastPayload(sample, new Payload.Light.On
                                    {
                                        LightId = "072"
                                    });
                                });

                                hub.Incoming<Payload.Navigation.Down>(x =>
                                {
                                    hub.BroadcastPayload(sample, new Payload.Light.Off
                                    {
                                        LightId = "072"
                                    });
                                });
                                */
                //hub.BroadcastPayload(sample, new Payload.Audio.SetInputOutput
                //    {
                //        Input = 3,
                //        Output = 3
                //    });



                //Console.ReadLine();
            }
        }

        public void Stop()
        {
        }

        private static HubConfig LoadHubConfig(string configPath)
        {
            HubConfig hubConfig;
            int configHash;
            string configContent;

            string hubConfigFile = Path.Combine(configPath, "Hub.json");
            if (!File.Exists(hubConfigFile))
            {
                hubConfig = new Config.HubConfig();
                configContent = JsonConvert.SerializeObject(hubConfig);
                configHash = configContent.GetHashCode();
                File.WriteAllText(hubConfigFile, configContent);
            }
            else
            {
                using (var file = File.OpenText(hubConfigFile))
                {
                    configContent = file.ReadToEnd();
                    configHash = configContent.GetHashCode();

                    hubConfig = JsonConvert.DeserializeObject<Config.HubConfig>(configContent);
                }
            }

            if (string.IsNullOrEmpty(hubConfig.DeviceId))
            {
                hubConfig.DeviceId = IoStorm.DeviceId.GetDeviceId();
            }

            var usedInstanceIds = new HashSet<string>();
            if (hubConfig.Plugins != null)
                foreach (var pluginConfig in hubConfig.Plugins)
                    ValidatePluginConfig(pluginConfig, usedInstanceIds);

            // Saving config
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            configContent = JsonConvert.SerializeObject(hubConfig, jsonSettings);

            if (configContent.GetHashCode() != configHash)
            {
                using (var file = File.CreateText(hubConfigFile))
                {
                    file.Write(configContent);
                }
                configHash = configContent.GetHashCode();
            }

            return hubConfig;
        }

        private static List<ZoneConfig> LoadZoneConfig(string configPath)
        {
            List<ZoneConfig> zoneConfigs;
            int configHash;
            string configContent;

            string zoneConfigFile = Path.Combine(configPath, "Zones.json");
            if (!File.Exists(zoneConfigFile))
            {
                zoneConfigs = new List<ZoneConfig>();
                zoneConfigs.Add(new ZoneConfig
                    {
                        Name = "House",
                        ZoneId = InstanceId.GetInstanceId()
                    });
                configContent = JsonConvert.SerializeObject(zoneConfigs);
                configHash = configContent.GetHashCode();
                File.WriteAllText(zoneConfigFile, configContent);
            }
            else
            {
                using (var file = File.OpenText(zoneConfigFile))
                {
                    configContent = file.ReadToEnd();
                    configHash = configContent.GetHashCode();

                    zoneConfigs = JsonConvert.DeserializeObject<List<ZoneConfig>>(configContent);
                }

                // Just in case
                if (zoneConfigs == null)
                    zoneConfigs = new List<ZoneConfig>();
            }

            // Check for invalid zone ids
            var usedZoneIds = new HashSet<string>();
            foreach (var zoneConfig in zoneConfigs)
                ValidateZoneConfig(zoneConfig, usedZoneIds);

            // Saving config
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            configContent = JsonConvert.SerializeObject(zoneConfigs, jsonSettings);

            if (configContent.GetHashCode() != configHash)
            {
                using (var file = File.CreateText(zoneConfigFile))
                {
                    file.Write(configContent);
                }

                // Hmm
                configHash = configContent.GetHashCode();
            }

            return zoneConfigs;
        }

        private static void ValidateZoneConfig(ZoneConfig zoneConfig, HashSet<string> usedZoneIds)
        {
            if (string.IsNullOrEmpty(zoneConfig.ZoneId))
                zoneConfig.ZoneId = InstanceId.GetInstanceId();

            if (usedZoneIds.Contains(zoneConfig.ZoneId))
            {
                string newZoneId = InstanceId.GetInstanceId();
                log.Warn("Duplicate ZoneId {0}, re-generating to {1}", zoneConfig.ZoneId, newZoneId);
                zoneConfig.ZoneId = newZoneId;
            }

            usedZoneIds.Add(zoneConfig.ZoneId);

            if (zoneConfig.Zones != null)
                foreach (var child in zoneConfig.Zones)
                    ValidateZoneConfig(child, usedZoneIds);
        }

        private static void ValidatePluginConfig(Config.PluginConfig pluginConfig, HashSet<string> usedInstanceIds)
        {
            if (string.IsNullOrEmpty(pluginConfig.InstanceId))
                pluginConfig.InstanceId = InstanceId.GetInstanceId();

            if (usedInstanceIds.Contains(pluginConfig.InstanceId))
            {
                string newZoneId = InstanceId.GetInstanceId();
                log.Warn("Duplicate InstanceId {0}, re-generating to {1} for plugin {2}", pluginConfig.InstanceId, newZoneId, pluginConfig.PluginId);
                pluginConfig.InstanceId = newZoneId;
            }

            usedInstanceIds.Add(pluginConfig.InstanceId);
        }

        private static string GetFullPath(string subFolder)
        {
            string assemblyLoc = Assembly.GetExecutingAssembly().Location;
            string currentDirectory = assemblyLoc.Substring(0, assemblyLoc.LastIndexOf(Path.DirectorySeparatorChar) + 1);

            if (string.IsNullOrEmpty(subFolder))
                return currentDirectory;

            if (Path.IsPathRooted(subFolder))
                return subFolder;

            return Path.Combine(currentDirectory, subFolder);
        }

        private static void LoadPlugins(StormHub hub, string zoneId, IEnumerable<Config.PluginConfig> pluginConfigs)
        {
            foreach (var pluginConfig in pluginConfigs)
            {
                if (pluginConfig.Disabled)
                    continue;

                try
                {
                    var plugin = hub.AvailablePlugins.SingleOrDefault(x => x.PluginId == pluginConfig.PluginId);
                    if (plugin == null)
                    {
                        log.Warn("Plugin {0} ({1}) not found", pluginConfig.PluginId, pluginConfig.Name);
                        continue;
                    }

                    log.Info("Loading plugin {0} ({1})", plugin.PluginId, plugin.Name);

                    var devInstance = hub.AddDeviceInstance(
                        plugin,
                        pluginConfig.Name,
                        pluginConfig.InstanceId,
                        zoneId,
                        pluginConfig.Settings);
                }
                catch (Exception ex)
                {
                    log.WarnException(ex, "Failed to load device {0} ({1})", pluginConfig.InstanceId, pluginConfig.Name);
                }
            }
        }
    }
}
