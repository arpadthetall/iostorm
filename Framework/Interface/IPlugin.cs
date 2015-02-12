using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using IoStorm.Addressing;

namespace IoStorm
{
    public interface IPlugin
    {
        PluginAddress InstanceId { get; }
    }
}
