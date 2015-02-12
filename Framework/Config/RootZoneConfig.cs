using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;
using IoStorm.Addressing;

namespace IoStorm.Config
{
    public class RootZoneConfig
    {
        public List<ZoneConfig> Zones { get; private set; }

        [JsonIgnore]
        public int LastSavedHashCode { get; set; }

        public RootZoneConfig()
        {
            Zones = new List<ZoneConfig>();
        }

        internal void Validate(ILog log, HashSet<ZoneAddress> usedZoneIds, HashSet<NodeAddress> usedNodeIds)
        {
            if (Zones == null)
                Zones = new List<ZoneConfig>();

            foreach (var child in Zones)
                child.Validate(log, usedZoneIds, usedNodeIds);
        }
    }
}
