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
        public string InstanceId { get; set; }

        public string PluginId { get; set; }

        public string Name { get; set; }
    }
}
