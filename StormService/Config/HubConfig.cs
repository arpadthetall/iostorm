using System.Collections.Generic;

namespace IoStorm.StormService
{
    public class HubConfig
    {
        public string DeviceId { get; set; }

        public string Name { get; set; }

        public string UpstreamHub { get; set; }

        public List<DeviceConfig> Devices { get; set; }

        public List<ZoneConfig> Zones { get; set; }

        public HubConfig()
        {
            Devices = new List<DeviceConfig>();
            Zones = new List<ZoneConfig>();
        }
    }
}
