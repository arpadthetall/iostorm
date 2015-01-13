using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;

namespace Storm.Sample1
{
    public class Program
    {
        private static IUnityContainer container;
        const string hubServer = "localhost";

        public static void Main(string[] args)
        {
            container = new UnityContainer();

            var logFactory = new Qlue.Logging.NLogFactoryProvider();
            container.RegisterInstance<Qlue.Logging.ILogFactory>(logFactory);

            Qlue.Logging.ILog log = logFactory.GetLogger("Main");

            log.Info("Start up");

            string command = string.Empty;

            if (args.Length > 0)
                command = args[0];

            string deviceId = Storm.DeviceId.GetDeviceId();

            using (var hub = new Storm.StormHub(container, deviceId, remoteHubHost: hubServer))
            {
                hub.LoadPlugin<Storm.Plugins.YamahaReceiver>();

                if (!string.IsNullOrEmpty(command))
                {
                    var irMan = hub.LoadPlugin<Storm.Plugins.IrmanReceiver>(new ParameterOverride("serialPortName", command));

                    // Map remote controls
                    Plugins.RemoteControlSB.MapRemoteControl(irMan);
                    Plugins.RemoteControlYamahaReceiver.MapRemoteControl(irMan);
                }

                hub.LoadPlugin<Storm.Sonos.Sonos>();

                // Test
                var sample = hub.LoadPlugin<Sample1>();
                hub.BroadcastPayload(sample, new Payload.Audio.ChangeVolume { Steps = 1 });

                Console.ReadLine();
            }
        }
    }
}
