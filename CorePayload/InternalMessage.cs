using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class InternalMessage
    {
        public string OriginatingZoneId { get; set; }

        public string DestinationZoneId { get; set; }

        public string OriginatingInstanceId { get; set; }

        public string DestinationInstanceId { get; set; }

        public IPayload Payload { get; set; }

        public InternalMessage(string originatingInstanceId, IPayload payload, string destinationInstanceId, string originatingZoneId, string destinationZoneId)
        {
            OriginatingInstanceId = originatingInstanceId;
            Payload = payload;
            DestinationInstanceId = destinationInstanceId;
            OriginatingZoneId = originatingZoneId;
            DestinationZoneId = destinationZoneId;
        }
    }
}
