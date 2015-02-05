using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload.Activity
{
    public class Feedback : BasePayload, IRemotePayload
    {
        public string CurrentActivityName { get; set; }
    }
}
