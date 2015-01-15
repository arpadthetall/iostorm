using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public abstract class BaseDevice : IDevice
    {
        public string InstanceId { get; private set; }

        public BaseDevice(string instanceId)
        {
            InstanceId = instanceId;
        }
    }
}
