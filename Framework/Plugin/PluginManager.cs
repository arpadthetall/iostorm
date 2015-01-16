using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Data;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Collections.Generic;

namespace IoStorm
{
    /// <summary>
    /// PluginManager
    /// </summary>
    /// <remarks>Derived from this work: http://www.codeproject.com/Articles/8832/Plug-in-Manager</remarks>
    internal class PluginManager<TPluginBase>
    {
        protected string pluginPath;
        protected AppDomain pluginAppDomain;
        protected RemoteLoader<TPluginBase> remoteLoader;
        protected List<AvailablePlugin> loadedTypes;

        /// <summary>
        /// Constructs a plugin manager
        /// </summary>
        /// <param name="pluginRelativePath">The relative path to the plugins directory</param>
        public PluginManager(string pluginFullPath, params string[] excludeAssemblies)
        {
            this.pluginPath = pluginFullPath;

            CreateAppDomainAndLoader(pluginFullPath);

            LoadUserAssemblies(pluginFullPath, excludeAssemblies);

            this.loadedTypes = this.remoteLoader.GetSubclasses().ToList();

            AppDomain.Unload(this.pluginAppDomain);
            this.pluginAppDomain = null;
        }

        private void CreateAppDomainAndLoader(string pluginDirectory)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationName = "Plugins";
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            setup.PrivateBinPath = Path.GetFileName(pluginDirectory);// Di Path.GetDirectoryName(pluginDirectory).Substring(
            //Path.GetDirectoryName(pluginDirectory).LastIndexOf(Path.DirectorySeparatorChar) + 1);
            //setup.CachePath = Path.Combine(pluginDirectory, "cache" + Path.DirectorySeparatorChar);
            //setup.ShadowCopyFiles = "true";
            //setup.ShadowCopyDirectories = pluginDirectory;

            this.pluginAppDomain = AppDomain.CreateDomain(
                "Plugins", null, setup);

            this.remoteLoader = (RemoteLoader<TPluginBase>)pluginAppDomain.CreateInstanceAndUnwrap(
                 Assembly.GetExecutingAssembly().FullName,
                typeof(RemoteLoader<TPluginBase>).FullName);
        }

        /// <summary>
        /// Loads all user created plugin assemblies
        /// </summary>
        protected void LoadUserAssemblies(string pluginDirectory, string[] excludeAssemblies)
        {
            DirectoryInfo directory = new DirectoryInfo(pluginDirectory);
            foreach (FileInfo file in directory.GetFiles("*.dll"))
            {
                if (excludeAssemblies.Contains(file.Name))
                    continue;

                try
                {
                    this.remoteLoader.LoadAssembly(file.FullName);
                }
                catch (PolicyException e)
                {
                    throw new PolicyException(
                        string.Format("Cannot load {0} - code requires privilege to execute", file.Name),
                        e);
                }
            }
        }

        /// <summary>
        /// Retrieves the type objects for all subclasses of the given type within the loaded plugins.
        /// </summary>
        /// <param name="baseClass">The base class</param>
        /// <returns>All subclases</returns>
        public IReadOnlyList<AvailablePlugin> GetAvailablePlugins()
        {
            return this.loadedTypes.AsReadOnly();
        }

        public Type LoadPluginType(string qualifiedAssembly)
        {
            return Type.GetType(qualifiedAssembly, (assemblyName) =>
                {
                    string assemblyFileName = assemblyName.Name + ".dll";
                    return Assembly.LoadFile(Path.Combine(this.pluginPath, assemblyFileName));
                }, null);
        }
    }
}
