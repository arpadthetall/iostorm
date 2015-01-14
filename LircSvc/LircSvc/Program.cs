using System.ServiceProcess;

namespace IoStorm.LircSvc
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[] 
            { 
                new LircSvc() 
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}
