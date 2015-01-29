using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Qlue.Logging;

namespace IoStorm.Config
{
    public class ConfigManager
    {
        private ILog log;
        private string configBasePath;

        public ConfigManager(ILogFactory logFactory, string configBasePath)
        {
            this.log = logFactory.GetLogger("ConfigManager");
            this.configBasePath = configBasePath;
        }

        private JsonSerializerSettings GetJsonSettings()
        {
            return new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                MissingMemberHandling = MissingMemberHandling.Ignore
            };
        }

        public HubConfig LoadHubConfig(string fileName = "Hub.json")
        {
            HubConfig hubConfig;

            string hubConfigFile = Path.Combine(this.configBasePath, fileName);
            string configContent;

            if (!File.Exists(hubConfigFile))
            {
                // This is where we create the default hub config
                hubConfig = new Config.HubConfig
                {
                    Name = "Hub",
                    UpstreamHub = string.Empty
                };
                configContent = JsonConvert.SerializeObject(hubConfig, GetJsonSettings());
            }
            else
                configContent = File.ReadAllText(hubConfigFile);

            hubConfig = JsonConvert.DeserializeObject<Config.HubConfig>(configContent, GetJsonSettings());
            hubConfig.LastSavedHashCode = configContent.GetHashCode();

            hubConfig.Validate(this.log);

            // Call Save in case Validate changed the config
            SaveHubConfig(hubConfig, fileName);

            return hubConfig;
        }

        public void SaveHubConfig(HubConfig hubConfig, string fileName = "Hub.json")
        {
            string hubConfigFile = Path.Combine(this.configBasePath, fileName);

            string configContent = JsonConvert.SerializeObject(hubConfig, GetJsonSettings());
            int configHash = configContent.GetHashCode();

            // Only save if we have changed
            if (hubConfig.LastSavedHashCode != configHash)
            {
                File.WriteAllText(hubConfigFile, configContent);
                hubConfig.LastSavedHashCode = configHash;
            }
        }

        public RootZoneConfig LoadRootZoneConfig(string fileName = "Zones.json")
        {
            RootZoneConfig rootZoneConfig;

            string zoneConfigFile = Path.Combine(this.configBasePath, fileName);
            string configContent;

            if (!File.Exists(zoneConfigFile))
            {
                // This is where we create the default zone config
                rootZoneConfig = new Config.RootZoneConfig();
                rootZoneConfig.Zones.Add(new ZoneConfig
                    {
                        Name = "Main"
                    });
                configContent = JsonConvert.SerializeObject(rootZoneConfig, GetJsonSettings());
            }
            else
                configContent = File.ReadAllText(zoneConfigFile);

            rootZoneConfig = JsonConvert.DeserializeObject<Config.RootZoneConfig>(configContent, GetJsonSettings());
            rootZoneConfig.LastSavedHashCode = configContent.GetHashCode();

            var usedZoneIds = new HashSet<string>();
            var usedNodeIds = new HashSet<string>();
            rootZoneConfig.Validate(this.log, usedZoneIds, usedNodeIds);

            // Call Save in case Validate changed the config
            SaveRootZoneConfig(rootZoneConfig, fileName);

            return rootZoneConfig;
        }

        public void SaveRootZoneConfig(RootZoneConfig rootZoneConfig, string fileName = "Zones.json")
        {
            string zoneConfigFile = Path.Combine(this.configBasePath, fileName);

            string configContent = JsonConvert.SerializeObject(rootZoneConfig, GetJsonSettings());
            int configHash = configContent.GetHashCode();

            // Only save if we have changed
            if (rootZoneConfig.LastSavedHashCode != configHash)
            {
                File.WriteAllText(zoneConfigFile, configContent);
                rootZoneConfig.LastSavedHashCode = configHash;
            }
        }
    }
}
