﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    [Serializable]
    public class PluginInstance
    {
        public string InstanceId { get; private set; }

        [Obsolete]
        public string ZoneId { get; set; }

        public string PluginId { get; private set; }

        public string Name { get; set; }

        public PluginInstance(string pluginId, string instanceId)
        {
            PluginId = pluginId;
            InstanceId = instanceId;
        }
    }
}