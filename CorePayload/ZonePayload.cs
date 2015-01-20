using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class ZonePayload : IPayload
    {
        public string ZoneId { get; set; }

        public IPayload Payload { get; set; }

        public string GetDebugInfo()
        {
            return string.Format("{0} from {1}", Payload.GetDebugInfo(), ZoneId);
        }
    }
}
