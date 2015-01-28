using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public interface IHub
    {
        void BroadcastPayload(IPlugin sender, Payload.IPayload payload);

        string GetSetting(IPlugin device, string key);

        string ZoneId { get; }

        [Obsolete]
        T LoadPlugin<T>(DeviceInstance deviceInstance) where T : IPlugin;
    }
}
