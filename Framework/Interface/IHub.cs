using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm
{
    public interface IHub
    {
        string ConfigPath { get; }

        void SendPayload(IPlugin sender, Payload.IPayload payload, StormAddress destination = null);

        void SendPayload(InstanceAddress originatingInstanceId, Payload.IPayload payload, StormAddress destination = null);

        string GetSetting(IPlugin device, string key, string defaultValue = null);

        T GetSetting<T>(IPlugin device, string key, T defaultValue = null) where T : class;

        [Obsolete]
        T LoadPlugin<T>(PluginInstance deviceInstance) where T : IPlugin;

        Payload.IPayload Rpc(Payload.IPayload request);
    }
}
