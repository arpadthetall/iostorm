using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.Payload
{
    public class InternalMessage
    {
        public string OriginatingInstanceId { get; set; }

        public IPayload Payload { get; set; }

        public InternalMessage(string originatingInstanceId, IPayload payload)
        {
            OriginatingInstanceId = originatingInstanceId;
            Payload = payload;
        }
    }
}
