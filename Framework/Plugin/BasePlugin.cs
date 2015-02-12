using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm
{
    public abstract class BasePlugin : IPlugin
    {
        public PluginAddress InstanceId { get; private set; }

        public BasePlugin(PluginAddress instanceId)
        {
            InstanceId = instanceId;
        }
    }
}
