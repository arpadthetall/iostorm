using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload.Activity
{
    public class SetRoute : BasePayload
    {
        public string IncomingInstanceId { get; set; }

        public string OutgoingInstanceId { get; set; }

        public List<string> Payloads { get; set; }
    }
}
