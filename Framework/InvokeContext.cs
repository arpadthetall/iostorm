using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public class InvokeContext
    {
        public string OriginDeviceId { get; internal set; }

        public string DestinationZoneId { get; set; }

        public IObserver<Payload.IPayload> Response { get; private set; }

        internal InvokeContext(IoStorm.Payload.InternalMessage intMessage)
        {
            OriginDeviceId = intMessage.OriginatingInstanceId;
            DestinationZoneId = intMessage.DestinationZoneId;
        }

        public InvokeContext(IObserver<Payload.IPayload> response = null)
        {
            Response = response;
        }
    }
}
