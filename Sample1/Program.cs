using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using PowerArgs;

namespace IoStorm.Sample1
{
    public class Program
    {
        private static IUnityContainer container;

        public class Arguments
        {
            public string UpbSerialPort { get; set; }

            public string AudioSwitcherSerialPort { get; set; }

            public string IrManSerialPort { get; set; }

            [ArgDefaultValue("localhost")]
            public string HubServer { get; set; }
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

            string deviceId = IoStorm.DeviceId.GetDeviceId();

            using (var hub = new IoStorm.StormHub(container, deviceId, remoteHubHost: arguments.HubServer))
            {
                if (!string.IsNullOrEmpty(arguments.UpbSerialPort))
                    hub.LoadPlugin<IoStorm.Plugins.UpbPim>(new ParameterOverride("serialPortName", arguments.UpbSerialPort));

                hub.LoadPlugin<IoStorm.Plugins.YamahaReceiver>();

                if (!string.IsNullOrEmpty(arguments.IrManSerialPort))
                {
                    var irMan = hub.LoadPlugin<IoStorm.Plugins.IrmanReceiver>(new ParameterOverride("serialPortName", arguments.IrManSerialPort));

                    // Map remote controls
                    RemoteMapping.IrManSony.MapRemoteControl(irMan);
                    RemoteMapping.IrManSqueezebox.MapRemoteControl(irMan);

                    var xlat = hub.LoadPlugin<IoStorm.RemoteMapping.ProtocolToPayload>();
                    xlat.MapSqueezeBoxRemote();
                }

                if (!string.IsNullOrEmpty(arguments.AudioSwitcherSerialPort))
                {
                    hub.LoadPlugin<Plugins.SerialSwitcher>(new ParameterOverride("serialPortName", arguments.AudioSwitcherSerialPort));
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

                //hub.BroadcastPayload(sample, new Payload.Audio.SetInputOutput
                //    {
                //        Input = 3,
                //        Output = 3
                //    });

                Console.ReadLine();
            }
        }
    }
}
