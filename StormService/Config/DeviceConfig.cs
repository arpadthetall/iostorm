using System.Collections.Generic;
using Newtonsoft.Json;

namespace IoStorm.StormService
{
    public class DeviceConfig
    {
        public string InstanceId { get; set; }

        public string PluginId { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Settings { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Disabled { get; set; }

        public DeviceConfig()
        {
            Settings = new Dictionary<string, string>();
        }
    }
}
