using System.Collections.Generic;

namespace IoStorm.StormService
{
    public class ZoneConfig
    {
        public string ZoneId { get; set; }

        public string Name { get; set; }

        public List<DeviceConfig> Devices { get; set; }

        public List<ZoneConfig> Zones { get; set; }

        public ZoneConfig()
        {
            Devices = new List<DeviceConfig>();
            Zones = new List<ZoneConfig>();
        }
    }
}
