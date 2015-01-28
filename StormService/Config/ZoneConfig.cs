using System;
using System.Collections.Generic;

namespace IoStorm.StormService.Config
{
    public class ZoneConfig
    {
        public string ZoneId { get; set; }

        public string Name { get; set; }

        public List<ZoneConfig> Zones { get; set; }

        public ZoneConfig()
        {
            Zones = new List<ZoneConfig>();
        }
    }
}
