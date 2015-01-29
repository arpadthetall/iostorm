using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.Plugin
{
    public class PluginManager
    {
        private ILog log;
        private PluginDiscovery<IPlugin> pluginDiscovery;
        private IReadOnlyList<AvailablePlugin> availablePlugins;

        public PluginManager(ILogFactory logFactory, string pluginPath)
        {
            this.log = logFactory.GetLogger("PluginManager");

            // Copy common dependencies
            string assemblyPath = AppDomain.CurrentDomain.BaseDirectory;
            File.Copy(Path.Combine(assemblyPath, "IoStorm.CorePayload.dll"), Path.Combine(pluginPath, "IoStorm.CorePayload.dll"), true);
            File.Copy(Path.Combine(assemblyPath, "IoStorm.Framework.dll"), Path.Combine(pluginPath, "IoStorm.Framework.dll"), true);

            AppDomain.CurrentDomain.AssemblyResolve += (sender, arg) =>
            {
                // Search plugins subfolder for plugins
                string[] parts = arg.Name.Split(',');
                if (parts.Length > 0)
                {
                    string pluginFolder = pluginPath;

                    string assemblyFileName = Path.Combine(pluginFolder, parts[0] + ".dll");
                    if (File.Exists(assemblyFileName))
                    {
                        return System.Reflection.Assembly.LoadFile(assemblyFileName);
                    }
                }
                return null;
            };

            this.pluginDiscovery = new PluginDiscovery<IPlugin>(pluginPath,
                "IoStorm.CorePayload.dll",
                "IoStorm.Framework.dll");

            this.availablePlugins = this.pluginDiscovery.GetAvailablePlugins();
        }

        public IReadOnlyList<AvailablePlugin> AvailablePlugins
        {
            get { return this.availablePlugins; }
        }

        public AvailablePlugin GetPluginTypeFromPluginId(string pluginId)
        {
            return this.pluginDiscovery.GetAvailablePlugins().FirstOrDefault(x => x.PluginId == pluginId);
        }

        public Type LoadPluginType(AvailablePlugin plugin)
        {
            return this.pluginDiscovery.LoadPluginType(plugin.AssemblyQualifiedName);
        }

        public void LoadPlugins(StormHub hub, string zoneId, IEnumerable<IoStorm.Config.PluginConfig> pluginConfigs)
        {
            foreach (var pluginConfig in pluginConfigs)
            {
                if (pluginConfig.Disabled)
                    continue;

                try
                {
                    var plugin = AvailablePlugins.SingleOrDefault(x => x.PluginId == pluginConfig.PluginId);
                    if (plugin == null)
                    {
                        log.Warn("Plugin {0} ({1}) not found", pluginConfig.PluginId, pluginConfig.Name);
                        continue;
                    }

                    log.Info("Loading plugin {0} ({1})", plugin.PluginId, plugin.Name);

                    var devInstance = hub.AddDeviceInstance(
                        plugin,
                        pluginConfig.Name,
                        pluginConfig.InstanceId,
                        zoneId,
                        pluginConfig.Settings);
                }
                catch (Exception ex)
                {
                    log.WarnException(ex, "Failed to load device {0} ({1})", pluginConfig.InstanceId, pluginConfig.Name);
                }
            }
        }
    }
}
