using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using PowerArgs;
using Newtonsoft.Json;
using System.IO;

namespace IoStorm.Sample1
{
    public class Program
    {
        private static IUnityContainer container;

        public class Arguments
        {
            [ArgExistingFile]
            [ArgShortcut("c")]
            public string ConfigFile { get; set; }
        }

        public static void Main(string[] args)
        {
            Arguments arguments;

            try
            {
                arguments = Args.Parse<Arguments>(args);
            }
            catch (ArgException ex)
            {
                Console.WriteLine(ex.Message);
                Console.WriteLine(ArgUsage.GenerateUsageFromTemplate<Arguments>());

                return;
            }

            container = new UnityContainer();

            var logFactory = new Qlue.Logging.NLogFactoryProvider();
            container.RegisterInstance<Qlue.Logging.ILogFactory>(logFactory);

            Qlue.Logging.ILog log = logFactory.GetLogger("Main");

            log.Info("Start up");

            log.Info("Config file {0}", arguments.ConfigFile);

            string deviceId = IoStorm.DeviceId.GetDeviceId();

            log.Info("Device Id {0}", deviceId);

            HubConfig hubConfig;
            using (var configFile = File.OpenText(arguments.ConfigFile))
            {
                hubConfig = JsonConvert.DeserializeObject<HubConfig>(configFile.ReadToEnd());

                if (hubConfig.Devices == null)
                    hubConfig.Devices = new List<DeviceConfig>();
            }

            log.Info("Connecting to remote hub at {0}", hubConfig.HubHostName);

            using (var hub = new IoStorm.StormHub(container, deviceId, remoteHubHost: hubConfig.HubHostName))
            {
                var plugins = hub.AvailablePlugins;

                foreach (var deviceConfig in hubConfig.Devices)
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
                        deviceConfig.Settings);
                }

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

                //Task.Delay(3000).ContinueWith(x =>
                //    {
                //        var sample = hub.LoadPlugin<Sample1>();
                //        hub.BroadcastPayload(sample, new Payload.Light.Off
                //        {
                //            LightId = "053"
                //        });
                //    });


                Console.ReadLine();
            }
        }
    }
}
