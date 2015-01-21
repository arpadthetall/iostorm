using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class ZonePayload : IPayload, IRemotePayload
    {
        public string ZoneId { get; set; }

        public IPayload Payload { get; set; }

        public string GetDebugInfo()
        {
            return string.Format("{0} from zone {1}", Payload.GetDebugInfo(), ZoneId);
        }
    }
}
