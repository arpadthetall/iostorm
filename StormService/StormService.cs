using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Config;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;

namespace IoStorm.StormService
{
    public class StormService
    {
        private IUnityContainer container;
        private Qlue.Logging.ILogFactory logFactory;
        private Qlue.Logging.ILog log;
        private string configPath;
        private string pluginPath;
        private IoStorm.StormHub hub;
        private Config.RootZoneConfig rootZoneConfig;

        public StormService()
        {
            string currentFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.configPath = GetFullPath(ConfigurationManager.AppSettings["ConfigFilePath"]);
            this.pluginPath = GetFullPath(ConfigurationManager.AppSettings["PluginFilePath"]);

            if (!Directory.Exists(this.configPath))
                Directory.CreateDirectory(this.configPath);
            if (!Directory.Exists(this.pluginPath))
                Directory.CreateDirectory(this.pluginPath);

            this.container = new UnityContainer();

            this.logFactory = new Qlue.Logging.NLogFactoryProvider();
            this.container.RegisterInstance<Qlue.Logging.ILogFactory>(this.logFactory);

            this.log = logFactory.GetLogger("Main");
        }

        public void Start()
        {
            log.Info("Start up");

            log.Info("Config file path {0}", this.configPath);
            log.Info("Plugin file path {0}", this.pluginPath);

            // Singletons (put in unity later?)
            var configManager = new ConfigManager(this.logFactory, this.configPath);
            var pluginManager = new IoStorm.Plugin.PluginManager(this.logFactory, this.pluginPath);

            foreach (var availablePlugin in pluginManager.AvailablePlugins)
                this.log.Info("Available Plugin: {0} - {1}", availablePlugin.PluginId, availablePlugin.Name);

            this.rootZoneConfig = configManager.LoadRootZoneConfig();
            Config.HubConfig hubConfig = configManager.LoadHubConfig();

            log.Info("Hub Device Id {0}", hubConfig.DeviceId);

            if (!string.IsNullOrEmpty(hubConfig.UpstreamHub))
                log.Info("Connecting to remote hub at {0}", hubConfig.UpstreamHub);

            this.hub = new IoStorm.StormHub(
                hubConfig: hubConfig,
                pluginManager: pluginManager,
                container: container);

            var activityController = this.hub.AddDeviceInstance<ActivityController>("Activity Controller",
                InstanceId.GetInstanceId(IoStorm.InstanceId.InstanceType_Plugin), hubConfig.DeviceId);

            if (hubConfig.IsDirty)
                configManager.SaveHubConfig(hubConfig);

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

        public void Stop()
        {
            if (this.hub != null)
            {
                this.hub.Dispose();
                this.hub = null;
            }
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
    }
}
