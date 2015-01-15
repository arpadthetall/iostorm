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
        protected AppDomain pluginAppDomain;
        protected RemoteLoader remoteLoader;
        protected List<Tuple<string, string>> loadedTypes;

        /// <summary>
        /// Constructs a plugin manager
        /// </summary>
        /// <param name="pluginRelativePath">The relative path to the plugins directory</param>
        public PluginManager(string pluginFullPath, params string[] excludeAssemblies)
        {
            this.loadedTypes = new List<Tuple<string, string>>();

            CreateAppDomainAndLoader(pluginFullPath);

            LoadUserAssemblies(pluginFullPath, excludeAssemblies);

            this.loadedTypes.AddRange(this.remoteLoader.GetSubclasses<TPluginBase>());

            AppDomain.Unload(this.pluginAppDomain);
            this.pluginAppDomain = null;
        }

        private void CreateAppDomainAndLoader(string pluginDirectory)
        {
            var setup = new AppDomainSetup();
            setup.ApplicationName = "Plugins";
            setup.ApplicationBase = AppDomain.CurrentDomain.BaseDirectory;
            setup.PrivateBinPath = Path.GetDirectoryName(pluginDirectory).Substring(
                Path.GetDirectoryName(pluginDirectory).LastIndexOf(Path.DirectorySeparatorChar) + 1);
            //setup.CachePath = Path.Combine(pluginDirectory, "cache" + Path.DirectorySeparatorChar);
            //setup.ShadowCopyFiles = "true";
            //setup.ShadowCopyDirectories = pluginDirectory;

            this.pluginAppDomain = AppDomain.CreateDomain(
                "Plugins", null, setup);

            this.remoteLoader = (RemoteLoader)pluginAppDomain.CreateInstanceAndUnwrap(
                 Assembly.GetExecutingAssembly().FullName,
                "IoStorm.RemoteLoader");
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
        public IEnumerable<Tuple<string, string>> GetSubclasses()
        {
            return this.loadedTypes;
        }

        public Type LoadPluginType(string qualifiedAssembly)
        {
            return Type.GetType(qualifiedAssembly);
        }
    }
}
