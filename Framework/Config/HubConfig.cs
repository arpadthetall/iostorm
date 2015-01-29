using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;

namespace IoStorm.Config
{
    public class HubConfig
    {
        private Dictionary<string, Config.PluginConfig> pluginSettings;
        private int lastSavedHashCode;

        public string DeviceId { get; set; }

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
            this.pluginSettings = new Dictionary<string, PluginConfig>();
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
            if (string.IsNullOrEmpty(DeviceId))
            {
                DeviceId = IoStorm.PhysicalDeviceId.GetDeviceId();
            }

            if (Plugins == null)
                Plugins = new List<PluginConfig>();

            var usedInstanceIds = new HashSet<string>();
            foreach (var pluginConfig in Plugins)
                pluginConfig.Validate(log, usedInstanceIds);
        }

        internal void PopulateDictionary()
        {
            lock (this)
            {
                foreach (var pluginConfig in Plugins)
                {
                    string key = pluginConfig.PluginId + ":" + pluginConfig.InstanceId;

                    this.pluginSettings[key] = pluginConfig;
                }
            }
        }

        internal Config.PluginConfig GetPluginConfig(string pluginId, string instanceId)
        {
            Config.PluginConfig pluginConfig;
            lock (this)
            {
                string key = pluginId + ":" + instanceId;
                if (!this.pluginSettings.TryGetValue(key, out pluginConfig))
                    throw new KeyNotFoundException("Instance settings not found");
            }

            return pluginConfig;
        }
    }
}
