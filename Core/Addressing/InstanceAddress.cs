using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Addressing
{
    [Serializable]
    public abstract class InstanceAddress : StormAddress
    {
        public InstanceAddress(AddressTypes addressType, string address)
            : base(addressType, address)
        {
        }
    }
}
