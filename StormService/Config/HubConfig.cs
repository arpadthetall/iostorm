using System;
using System.Collections.Generic;

namespace IoStorm.StormService.Config
{
    public class HubConfig
    {
        public string DeviceId { get; set; }

        public string Name { get; set; }

        public string UpstreamHub { get; set; }

        public List<PluginConfig> Devices { get; set; }

        public List<ZoneConfig> Zones { get; set; }

        public HubConfig()
        {
            Devices = new List<PluginConfig>();
            Zones = new List<ZoneConfig>();
        }
    }
}
