using System;
using System.ServiceProcess;

namespace IoStorm.StormService
{
    public partial class Service
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals("-console"))
            {
                Console.WriteLine("press 'q' to quit.");

                var service = new StormService();
                service.Start();

                while (Console.ReadKey().KeyChar != 'q')
                {
                }

                service.Stop();
            }
            else
            {
                var servicesToRun = new ServiceBase[] { new Service() };
                Run(servicesToRun);
            }
        }
    }
}
