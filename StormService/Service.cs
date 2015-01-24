using System.ServiceProcess;

namespace IoStorm.StormService
{
    public partial class Service : ServiceBase
    {
        private readonly StormService _service;

        public Service()
        {
            InitializeComponent();
            _service = new StormService();
        }

        protected override void OnStart(string[] args)
        {
            _service.Start();
        }

        protected override void OnStop()
        {
        }
    }
}
