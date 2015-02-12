using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm.Payload.Activity
{
    public class ClearRoute : BasePayload
    {
        public InstanceAddress[] IncomingInstanceId { get; set; }
    }
}
