using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;

namespace IoStorm.Config
{
    public class NodeConfig
    {
        public string InstanceId { get; set; }

        // Available via this plugin instance id
        public string PluginInstanceId { get; set; }

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

        internal void Validate(ILog log, HashSet<string> usedInstanceIds)
        {
            if (string.IsNullOrEmpty(InstanceId))
                InstanceId = IoStorm.InstanceId.GetInstanceId(IoStorm.InstanceId.InstanceType_Node);

            if (usedInstanceIds.Contains(InstanceId))
            {
                string newZoneId = IoStorm.InstanceId.GetInstanceId(IoStorm.InstanceId.InstanceType_Node);
                log.Warn("Duplicate InstanceId {0}, re-generating to {1} for node {2}", InstanceId, newZoneId, Name);
                InstanceId = newZoneId;
            }

            usedInstanceIds.Add(InstanceId);

            if (Settings == null)
                Settings = new Dictionary<string, string>();
        }
    }
}
