using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public interface IHub
    {
        string ConfigPath { get; }

        void SendPayload(IPlugin sender, Payload.IPayload payload, string destinationZoneId = null, string destinationInstanceId = null);

        void SendPayload(string originatingInstanceId, Payload.IPayload payload, string destinationZoneId = null, string destinationInstanceId = null);

        string GetSetting(IPlugin device, string key, string defaultValue = null);

        [Obsolete]
        T LoadPlugin<T>(PluginInstance deviceInstance) where T : IPlugin;

        Payload.IPayload Rpc(Payload.IPayload request);
    }
}
