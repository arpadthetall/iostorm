using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    [Serializable]
    public class AvailablePlugin
    {
        public string PluginId { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string Author { get; set; }

        internal string AssemblyQualifiedName { get; set; }

        public override string ToString()
        {
            return string.Format("Plugin {0}", PluginId);
        }
    }
}
