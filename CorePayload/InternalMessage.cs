using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm.Payload
{
    public class InternalMessage
    {
        public InstanceAddress Originating { get; set; }

        public StormAddress Destination { get; set; }

        public IPayload Payload { get; set; }

        public InternalMessage(InstanceAddress originatingInstanceId, IPayload payload, StormAddress destination)
        {
            Originating = originatingInstanceId;
            Payload = payload;
            Destination = destination;
        }
    }
}
