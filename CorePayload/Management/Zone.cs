using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm.Payload.Management
{
    public class Zone
    {
        public ZoneAddress ZoneId { get; set; }

        public string Name { get; set; }

        public List<Zone> Zones { get; set; }

        public Zone()
        {
            Zones = new List<Zone>();
        }
    }
}
