using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public interface IHub
    {
        void BroadcastPayload(IDevice sender, Payload.IPayload payload);

        string GetSetting(IDevice device, string key);
    }
}
