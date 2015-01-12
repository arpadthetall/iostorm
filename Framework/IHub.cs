using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm
{
    public interface IHub
    {
        void BroadcastPayload(IThing sender, Payload.IPayload payload);
    }
}
