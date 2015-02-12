using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;
using IoStorm.Addressing;

namespace IoStorm.Config
{
    public class HubConfig
    {
        private Dictionary<Tuple<string, InstanceAddress>, Config.PluginConfig> pluginSettings;
        private int lastSavedHashCode;

        [JsonConverter(typeof(IoStorm.Addressing.JsonAddressConverter))]
        public IoStorm.Addressing.HubAddress DeviceId { get; set; }

        public string Name { get; set; }

        public string UpstreamHub { get; set; }

        public List<PluginConfig> Plugins { get; private set; }

        [JsonIgnore]
        public int LastSavedHashCode
        {
            get { return this.lastSavedHashCode; }
        }

        public void ResetDirtyFlag(int lastSavedHashCode)
        {
            this.lastSavedHashCode = lastSavedHashCode;
            foreach (var pluginConfig in Plugins)
                pluginConfig.ResetDirtyFlag();
        }

        public HubConfig()
        {
            Plugins = new List<PluginConfig>();
            this.pluginSettings = new Dictionary<Tuple<string, InstanceAddress>, PluginConfig>();
        }

        [JsonIgnore]
        public bool IsDirty
        {
            get
            {
                foreach (var pluginConfig in Plugins)
                    if (pluginConfig.IsDirty)
                        return true;

                return false;
            }
        }

        internal void Validate(ILog log)
        {
            if (DeviceId == null)
            {
                DeviceId = IoStorm.PhysicalDeviceId.GetHubAddress();
            }

            if (Plugins == null)
                Plugins = new List<PluginConfig>();

            var usedInstanceIds = new HashSet<IoStorm.Addressing.InstanceAddress>();
            foreach (var pluginConfig in Plugins)
                pluginConfig.Validate(log, usedInstanceIds);
        }

        internal void PopulateDictionary()
        {
            lock (this)
            {
                foreach (var pluginConfig in Plugins)
                {
                    var key = Tuple.Create(pluginConfig.PluginId, (InstanceAddress)pluginConfig.InstanceId);

                    this.pluginSettings[key] = pluginConfig;
                }
            }
        }

        internal Config.PluginConfig GetPluginConfig(string pluginId, InstanceAddress instanceId)
        {
            Config.PluginConfig pluginConfig;
            lock (this)
            {
                var key = Tuple.Create(pluginId, instanceId);
                if (!this.pluginSettings.TryGetValue(key, out pluginConfig))
                    throw new KeyNotFoundException("Instance settings not found");
            }

            return pluginConfig;
        }
    }
}
