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
    internal class RemoteLoader : MarshalByRefObject
    {
        protected List<Type> typeList = new List<Type>();

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
                if (!loadedType.IsAbstract)
                    typeList.Add(loadedType);
            }
        }

        /// <summary>
        /// Retrieves the type objects for all subclasses of the given type within the loaded plugins.
        /// </summary>
        /// <returns>All subclases</returns>
        public Tuple<string, string>[] GetSubclasses<T>()
        {
            var classList = this.typeList
                .Where(x => typeof(T).IsAssignableFrom(x))
                .Select(x => Tuple.Create(x.FullName, x.AssemblyQualifiedName));

            return classList.ToArray();
        }
    }
}
