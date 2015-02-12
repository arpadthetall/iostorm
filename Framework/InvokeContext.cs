using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm
{
    public class InvokeContext
    {
        public InstanceAddress Originating { get; internal set; }

        public StormAddress Destination { get; set; }

        public IObserver<Payload.IPayload> Response { get; private set; }

        internal InvokeContext(IoStorm.Payload.InternalMessage intMessage)
        {
            Originating = intMessage.Originating;
            Destination = intMessage.Destination;
        }

        public InvokeContext(IObserver<Payload.IPayload> response = null)
        {
            Response = response;
        }
    }
}
