using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm.Payload
{
    public class BusPayload
    {
        public InstanceAddress OriginDeviceId { get; set; }

        public IPayload Payload { get; set; }
    }
}
