﻿using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Qlue.Logging;

namespace IoStorm.Config
{
    public class PluginConfig
    {
        private bool dirty;

        public string InstanceId { get; set; }

        public string PluginId { get; set; }

        public string Name { get; set; }

        public Dictionary<string, string> Settings { get; private set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
        public bool Disabled { get; set; }

        public PluginConfig()
        {
            Settings = new Dictionary<string, string>();
        }

        internal void Validate(ILog log, HashSet<string> usedInstanceIds)
        {
            if (string.IsNullOrEmpty(InstanceId))
                InstanceId = IoStorm.InstanceId.GetInstanceId(IoStorm.InstanceId.InstanceType_Plugin);

            if (usedInstanceIds.Contains(InstanceId))
            {
                string newZoneId = IoStorm.InstanceId.GetInstanceId(IoStorm.InstanceId.InstanceType_Plugin);
                log.Warn("Duplicate InstanceId {0}, re-generating to {1} for plugin {2}", InstanceId, newZoneId, PluginId);
                InstanceId = newZoneId;
            }

            usedInstanceIds.Add(InstanceId);

            if (Settings == null)
                Settings = new Dictionary<string, string>();
        }

        public string GetSetting(string key, string defaultValue)
        {
            string value;

            lock (this)
            {
                if (Settings.TryGetValue(key, out value))
                    return value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Save setting for this plugin instance
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>True if the settings collection is dirty/modified</returns>
        public bool SetSetting(string key, string value)
        {
            lock (this)
            {
                string currentValue;
                if (Settings.TryGetValue(key, out currentValue))
                {
                    // Already exists
                    if (string.Equals(currentValue, value))
                        // Not modified
                        return false;
                }

                Settings[key] = value;
                this.dirty = true;
            }

            return true;
        }

        internal void ResetDirtyFlag()
        {
            this.dirty = false;
        }

        internal bool IsDirty
        {
            get { return this.dirty; }
        }
    }
}
