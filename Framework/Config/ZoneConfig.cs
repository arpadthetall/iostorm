using System;
using System.Collections.Generic;
using Qlue.Logging;
using IoStorm.Addressing;

namespace IoStorm.Config
{
    public class ZoneConfig
    {
        public ZoneAddress ZoneId { get; set; }

        public string Name { get; set; }

        public List<ZoneConfig> Zones { get; private set; }

        public List<NodeConfig> Nodes { get; private set; }

        public ZoneConfig()
        {
            Zones = new List<ZoneConfig>();
            Nodes = new List<NodeConfig>();
        }

        internal void Validate(ILog log, HashSet<ZoneAddress> usedZoneIds, HashSet<NodeAddress> usedNodeIds)
        {
            if (ZoneId == null)
                ZoneId = InstanceId.GetZoneAddress<ZoneAddress>();

            if (usedZoneIds.Contains(ZoneId))
            {
                var newZoneId = InstanceId.GetZoneAddress<ZoneAddress>();
                log.Warn("Duplicate ZoneId {0}, re-generating to {1}", ZoneId, newZoneId);
                ZoneId = newZoneId;
            }

            ZoneId.DebugInfo = Name;

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
