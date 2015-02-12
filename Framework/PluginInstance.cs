using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    [Serializable]
    public class PluginInstance
    {
        public Addressing.PluginAddress InstanceId { get; private set; }

        public string PluginId { get; private set; }

        public string Name { get; set; }

        public PluginInstance(string pluginId, Addressing.PluginAddress instanceId)
        {
            PluginId = pluginId;
            InstanceId = instanceId;
        }
    }
}
