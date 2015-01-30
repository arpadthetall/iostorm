using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class RPCPayload
    {
        public IPayload Request { get; set; }

        public IPayload Response { get; set; }
    }
}
