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
        }

        public void Incoming(Payload.OscMessage payload)
        {
            if (payload.Address == "/1/toggle1")
            {
                this.hub.BroadcastPayload(this, new Payload.ZoneDestinationPayload
                {
                    DestinationZoneId = "a5d4023d3826454daac1c215eda5f0f0",
                    Payload = new Payload.IRCommand
                    {
                        PortId = "1",
                        Repeat = 2,
                        //Command = new IoStorm.IRProtocol.Nokia32(35, 64, payload.Value == "1" ? 88 : 89, 38, true)
                        //Command = new IoStorm.IRProtocol.Sony12(1, payload.Value == "1" ? 46 : 47)
                        Command = new IoStorm.IRProtocol.NECx(7, 7, payload.Value == "1" ? 153 : 152)
                    }
                });
            }
        }
    }
}
