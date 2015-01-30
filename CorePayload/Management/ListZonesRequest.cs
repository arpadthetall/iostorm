using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload.Management
{
    public class ListZonesResponse : BasePayload
    {
        public List<Zone> Zones { get; set; }
    }
}
