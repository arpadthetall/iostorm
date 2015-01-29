using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;

namespace IoStorm.Config
{
    public class HubConfig
    {
        public string DeviceId { get; set; }

        public string Name { get; set; }

        public string UpstreamHub { get; set; }

        public List<PluginConfig> Plugins { get; private set; }

        [JsonIgnore]
        public int LastSavedHashCode { get; set; }

        public HubConfig()
        {
            Plugins = new List<PluginConfig>();
        }

        internal void Validate(ILog log)
        {
            if (string.IsNullOrEmpty(DeviceId))
            {
                DeviceId = IoStorm.DeviceId.GetDeviceId();
            }

            if (Plugins == null)
                Plugins = new List<PluginConfig>();

            var usedInstanceIds = new HashSet<string>();
            foreach (var pluginConfig in Plugins)
                pluginConfig.Validate(log, usedInstanceIds);
        }
    }
}
