using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class InternalMessage
    {
        public string OriginatingInstanceId { get; set; }

        public string DestinationInstanceId { get; set; }

        public IPayload Payload { get; set; }

        public InternalMessage(string originatingInstanceId, IPayload payload, string destinationInstanceId)
        {
            OriginatingInstanceId = originatingInstanceId;
            Payload = payload;
            DestinationInstanceId = destinationInstanceId;
        }

        public InternalMessage(string originatingInstanceId, IPayload payload)
            : this(originatingInstanceId, payload, null)
        {
        }
    }
}
