using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IoStorm.PluginConfig
{
    public class InstanceSettings
    {
        private bool dirty;
        private Dictionary<string, string> settings;

        public string InstanceId { get; private set; }

        public InstanceSettings(string instanceId)
        {
            InstanceId = instanceId;

            this.settings = new Dictionary<string, string>();
        }

        public string GetSetting(string key, string defaultValue)
        {
            string value;

            lock (this)
            {
                if (this.settings.TryGetValue(key, out value))
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
                if (this.settings.TryGetValue(key, out currentValue))
                {
                    // Already exists
                    if (string.Equals(currentValue, value))
                        // Not modified
                        return false;
                }

                this.settings[key] = value;
                this.dirty = true;
            }

            return true;
        }

        internal void ResetDirtyFlag()
        {
            this.dirty = false;
        }

        internal string GetJson()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };

            return JsonConvert.SerializeObject(this.settings, jsonSettings);
        }

        internal void SetFromJson()
        {
            var jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented
            };
        }
    }
}
