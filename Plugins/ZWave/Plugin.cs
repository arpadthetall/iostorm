using System.Configuration;
using IoStorm.Plugin;
using ZWaveApi.Net;

namespace IoStorm.Plugins.ZWave
{
    [Plugin(Name = "ZWave", Description = "ZWave", Author = "IoStorm")]
    public class Plugin : BaseDevice
    {
        private readonly ZWaveController _controller = null;
        private Qlue.Logging.ILog log;
        private IHub hub;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            var comPort = this.hub.GetSetting(this, "ZWaveComPortB");

            this.hub = hub;

            this.log = logFactory.GetLogger("ZWave");

            _controller = new ZWaveController(comPort);
            _controller.Open();
        }
    }
}
