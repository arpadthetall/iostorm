using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public interface IHub
    {
        void BroadcastPayload(IPlugin sender, Payload.IPayload payload, string sourceZoneId = null);

        string GetSetting(IPlugin device, string key, string defaultValue = null);

        [Obsolete]
        T LoadPlugin<T>(PluginInstance deviceInstance) where T : IPlugin;
    }
}
