﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
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
            var configFilePath = ConfigurationManager.AppSettings["ConfigFilePath"];

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
                    string pluginFolder = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                        "Plugins");

                    string assemblyFileName = Path.Combine(pluginFolder, parts[0] + ".dll");
                    if (File.Exists(assemblyFileName))
                    {
                        return Assembly.LoadFile(assemblyFileName);
                    }
                }
                return null;
            };

            log.Info("Config file {0}", configFilePath);

            Config.HubConfig hubConfig;
            int configHash;
            string configContent;

            if (!File.Exists(configFilePath))
            {
                hubConfig = new Config.HubConfig();
                var config = JsonConvert.SerializeObject(hubConfig);
                File.WriteAllText(configFilePath, config);
            }

            using (var file = File.OpenText(configFilePath))
            {
                configContent = file.ReadToEnd();
                configHash = configContent.GetHashCode();

                hubConfig = JsonConvert.DeserializeObject<Config.HubConfig>(configContent);
            }

            if (string.IsNullOrEmpty(hubConfig.DeviceId))
            {
                hubConfig.DeviceId = IoStorm.DeviceId.GetDeviceId();
            }

            ValidateConfig(hubConfig.Devices, hubConfig.Zones);

            // Saving config
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
            configContent = JsonConvert.SerializeObject(hubConfig, jsonSettings);

            if (configContent.GetHashCode() != configHash)
            {
                using (var file = File.CreateText(configFilePath))
                {
                    file.Write(configContent);
                }
                configHash = configContent.GetHashCode();
            }

            log.Info("Device Id {0}", hubConfig.DeviceId);

            log.Info("Connecting to remote hub at {0}", hubConfig.UpstreamHub);

            using (var hub = new IoStorm.StormHub(container, hubConfig.DeviceId, remoteHubHost: hubConfig.UpstreamHub))
            {
                //                var plugins = hub.AvailablePlugins;

                LoadDevices(hub, hubConfig.DeviceId, hubConfig.Devices, hubConfig.Zones);

                var activityController = hub.AddDeviceInstance<ActivityController>("Activity Controller",
                    Guid.NewGuid().ToString("x"), hubConfig.DeviceId, null);

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

        private static void ValidateConfig(IEnumerable<DeviceConfig> devices, IEnumerable<ZoneConfig> zones)
        {
            foreach (var deviceConfig in devices)
            {
                if (string.IsNullOrEmpty(deviceConfig.InstanceId))
                    deviceConfig.InstanceId = Guid.NewGuid().ToString("n");
            }

            foreach (var zoneConfig in zones)
            {
                if (string.IsNullOrEmpty(zoneConfig.ZoneId))
                    zoneConfig.ZoneId = Guid.NewGuid().ToString("n");

                ValidateConfig(zoneConfig.Devices, zoneConfig.Zones);
            }
        }

        private static void LoadDevices(StormHub hub, string zoneId, IEnumerable<Config.PluginConfig> devices, IEnumerable<Config.ZoneConfig> zones)
        {
            foreach (var deviceConfig in devices)
            {
                if (deviceConfig.Disabled)
                    continue;

                try
                {
                    var plugin = hub.AvailablePlugins.SingleOrDefault(x => x.PluginId == deviceConfig.PluginId);
                    if (plugin == null)
                    {
                        log.Warn("Plugin {0} ({1}) not found", deviceConfig.PluginId, deviceConfig.Name);
                        continue;
                    }

                    log.Info("Loading plugin {0} ({1})", plugin.PluginId, plugin.Name);

                    var devInstance = hub.AddDeviceInstance(
                        plugin,
                        deviceConfig.Name,
                        deviceConfig.InstanceId,
                        zoneId,
                        deviceConfig.Settings);
                }
                catch (Exception ex)
                {
                    log.WarnException(ex, "Failed to load device {0} ({1})", deviceConfig.InstanceId, deviceConfig.Name);
                }
            }

            foreach (var zoneConfig in zones)
            {
                LoadDevices(hub, zoneConfig.ZoneId, zoneConfig.Devices, zoneConfig.Zones);
            }
        }
    }
}
