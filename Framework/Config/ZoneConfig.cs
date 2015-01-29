using System;
using System.Collections.Generic;
using Qlue.Logging;

namespace IoStorm.Config
{
    public class ZoneConfig
    {
        public string ZoneId { get; set; }

        public string Name { get; set; }

        public List<ZoneConfig> Zones { get; private set; }

        public List<NodeConfig> Nodes { get; private set; }

        public ZoneConfig()
        {
            Zones = new List<ZoneConfig>();
            Nodes = new List<NodeConfig>();
        }

        internal void Validate(ILog log, HashSet<string> usedZoneIds, HashSet<string> usedNodeIds)
        {
            if (string.IsNullOrEmpty(ZoneId))
                ZoneId = InstanceId.GetInstanceId(InstanceId.InstanceType_Zone);

            if (usedZoneIds.Contains(ZoneId))
            {
                string newZoneId = InstanceId.GetInstanceId(InstanceId.InstanceType_Zone);
                log.Warn("Duplicate ZoneId {0}, re-generating to {1}", ZoneId, newZoneId);
                ZoneId = newZoneId;
            }

            usedZoneIds.Add(ZoneId);

            if (Zones == null)
                Zones = new List<ZoneConfig>();

            if (Nodes == null)
                Nodes = new List<NodeConfig>();

            foreach (var node in Nodes)
                node.Validate(log, usedNodeIds);

            foreach (var child in Zones)
                child.Validate(log, usedZoneIds, usedNodeIds);
        }
    }
}
