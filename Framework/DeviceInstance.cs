using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    [Serializable]
    public class DeviceInstance
    {
        public string InstanceId { get; private set; }

        public string ZoneId { get; set; }

        public string PluginId { get; private set; }

        public string Name { get; set; }

        public DeviceInstance(string pluginId, string instanceId)
        {
            PluginId = pluginId;
            InstanceId = instanceId;
        }
    }
}
