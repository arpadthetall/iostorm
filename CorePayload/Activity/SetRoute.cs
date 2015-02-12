using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm.Payload.Activity
{
    public class SetRoute : BasePayload
    {
        public List<InstanceAddress> IncomingInstanceId { get; set; }

        public StormAddress OutgoingInstanceId { get; set; }

        public List<string> Payloads { get; set; }
    }
}
