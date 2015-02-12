using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Addressing
{
    [Serializable]
    public class PluginAddress : InstanceAddress
    {
        public PluginAddress(string address)
            : base(AddressTypes.Plugin, address)
        {
        }

        protected PluginAddress(AddressTypes addressType, string address)
            : base(addressType, address)
        {
        }
    }
}
