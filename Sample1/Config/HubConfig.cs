using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Sample1
{
    public class HubConfig
    {
        public string HubHostName { get; set; }

        public List<DeviceConfig> Devices { get; set; }
    }
}
