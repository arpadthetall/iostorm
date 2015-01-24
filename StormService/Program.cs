using System.ServiceProcess;

namespace IoStorm.StormService
{
    public partial class Service
    {
        static private NoServiceForm _noServiceForm;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("-noservice"))
            {
                _noServiceForm = new NoServiceForm {Text = "StormService"};

                var service = new Service();
                service.OnStart(new string[1]);

                _noServiceForm.ShowDialog();
                service.OnStop();
            }
            else
            {
                var servicesToRun = new ServiceBase[] { new Service() };
                Run(servicesToRun);
            }
        }
    }
}
