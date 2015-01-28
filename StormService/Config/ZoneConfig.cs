using System;
using System.Collections.Generic;

namespace IoStorm.StormService.Config
{
    public class ZoneConfig
    {
        public string ZoneId { get; set; }

        public string Name { get; set; }

        public List<PluginConfig> Devices { get; set; }

        public List<ZoneConfig> Zones { get; set; }

        public ZoneConfig()
        {
            Devices = new List<PluginConfig>();
            Zones = new List<ZoneConfig>();
        }
    }
}
