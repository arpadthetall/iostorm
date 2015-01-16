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
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace IoStorm
{
    /// <summary>
    /// The remote loader loads assumblies into a remote <see cref="AppDomain"/>
    /// </summary>
    internal class RemoteLoader<T> : MarshalByRefObject
    {
        protected List<AvailablePlugin> typeList = new List<AvailablePlugin>();

        /// <summary>
        /// Creates a remote assembly loader
        /// </summary>
        public RemoteLoader()
        {
        }

        /// <summary>
        /// Loads the assembly into the remote domain
        /// </summary>
        /// <param name="fullname">The full filename of the assembly to load</param>
        public void LoadAssembly(string fullname)
        {
            string path = Path.GetDirectoryName(fullname);
            string filename = Path.GetFileNameWithoutExtension(fullname);

            var assembly = Assembly.Load(filename);
            foreach (Type loadedType in assembly.GetTypes())
            {
                if (!loadedType.IsAbstract && typeof(T).IsAssignableFrom(loadedType))
                {
                    var availablePlugin = new AvailablePlugin
                    {
                        PluginId = loadedType.FullName,
                        AssemblyQualifiedName = loadedType.AssemblyQualifiedName
                    };

                    var attributes = loadedType.GetCustomAttribute<Plugin.PluginAttribute>(true);

                    if (attributes != null)
                    {
                        availablePlugin.Author = attributes.Author;
                        availablePlugin.Description = attributes.Description;
                        availablePlugin.Name = attributes.Name;
                    }

                    typeList.Add(availablePlugin);
                }
            }
        }

        /// <summary>
        /// Retrieves the type objects for all subclasses of the given type within the loaded plugins.
        /// </summary>
        /// <returns>All subclases</returns>
        public AvailablePlugin[] GetSubclasses()
        {
            return this.typeList.ToArray();
        }
    }
}
