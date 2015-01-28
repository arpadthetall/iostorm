using System;
using System.Collections.Generic;

namespace IoStorm.StormService.Config
{
    public class HubConfig
    {
        public string DeviceId { get; set; }

        public string Name { get; set; }

        public string UpstreamHub { get; set; }

        public List<PluginConfig> Plugins { get; set; }

        public HubConfig()
        {
            Plugins = new List<PluginConfig>();
        }
    }
}
