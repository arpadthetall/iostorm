using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Qlue.Logging;

namespace IoStorm
{
    public class ActivityController : BaseDevice
    {
        private ILog log;
        private IHub hub;

        public ActivityController(ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.log = logFactory.GetLogger("ActivityController");
            this.hub = hub;

            Task.Delay(3000).ContinueWith(x =>
            {
                this.hub.BroadcastPayload(this, new Payload.ZoneDestinationPayload
                {
                    DestinationZoneId = hub.ZoneId,
                    Payload = new Payload.IRCommand
                {
                    PortId = "1",
                    Repeat = 2,
                    Command = new IoStorm.IRProtocol.NECx(0xE0E0, 0x40BF)
                }
                });
            });
        }

    }
}
