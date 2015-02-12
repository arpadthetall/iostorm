using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;
using IoStorm.Addressing;

namespace IoStorm.Config
{
    public class NodeConfig
    {
        public IoStorm.Addressing.NodeAddress InstanceId { get; set; }

        // Available via this plugin instance id
        public PluginAddress PluginInstanceId { get; set; }

        public string Name { get; set; }

        // ??
        public string Type { get; set; }

        public Dictionary<string, string> Settings { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Disabled { get; set; }

        public NodeConfig()
        {
            Settings = new Dictionary<string, string>();
        }

        internal void Validate(ILog log, HashSet<NodeAddress> usedInstanceIds)
        {
            if (InstanceId == null)
                InstanceId = IoStorm.InstanceId.GetInstanceId<IoStorm.Addressing.NodeAddress>();

            if (usedInstanceIds.Contains(InstanceId))
            {
                var newNodeId = IoStorm.InstanceId.GetInstanceId<NodeAddress>();
                log.Warn("Duplicate InstanceId {0}, re-generating to {1} for node {2}", InstanceId, newNodeId, Name);
                InstanceId = newNodeId;
            }

            InstanceId.DebugInfo = Name;

            usedInstanceIds.Add(InstanceId);

            if (Settings == null)
                Settings = new Dictionary<string, string>();
        }
    }
}
