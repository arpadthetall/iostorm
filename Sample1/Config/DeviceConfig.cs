using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IoStorm.Sample1
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
