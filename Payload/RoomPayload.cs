using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class RoomPayload
    {
        public string Room { get; set; }

        public IPayload Payload { get; set; }
    }
}
