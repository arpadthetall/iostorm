using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class RPCPayload
    {
        public IoStorm.Addressing.InstanceAddress OriginDeviceId { get; set; }

        public IPayload Request { get; set; }
    }
}
