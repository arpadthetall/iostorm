using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace IoStorm.Sample2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var logFactory = new Qlue.Logging.NLogFactoryProvider();

            Qlue.Logging.ILog log = logFactory.GetLogger("Main");

            log.Info("Start up");

            var deviceId = IoStorm.InstanceId.GetInstanceId<IoStorm.Addressing.HubAddress>();

            using (var remoteHub = new RemoteHub(logFactory, "192.168.1.113", deviceId))
            {
                Application.Run(new Form1(remoteHub));
            }
        }
    }
}
