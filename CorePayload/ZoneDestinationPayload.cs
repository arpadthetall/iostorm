using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class ZoneDestinationPayload : IPayload, IRemotePayload
    {
        public string DestinationZoneId { get; set; }

        public IPayload Payload { get; set; }

        public string GetDebugInfo()
        {
            return string.Format("{0} to zone {1}", Payload.GetDebugInfo(), DestinationZoneId);
        }
    }
}
