using System.ServiceProcess;

namespace IoStorm.StormService
{
    public partial class StormService
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

                var service = new StormService();
                service.OnStart(new string[1]);

                _noServiceForm.ShowDialog();
                service.OnStop();
            }
            else
            {
                var servicesToRun = new ServiceBase[] { new StormService() };
                Run(servicesToRun);
            }
        }
    }
}
