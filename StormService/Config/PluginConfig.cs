using System.Collections.Generic;
using Newtonsoft.Json;

namespace IoStorm.StormService.Config
{
    public class PluginConfig
    {
        public string InstanceId { get; set; }

        public string PluginId { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Settings { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Disabled { get; set; }

        public PluginConfig()
        {
            Settings = new Dictionary<string, string>();
        }
    }
}
