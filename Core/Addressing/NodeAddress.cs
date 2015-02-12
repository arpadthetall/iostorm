using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Addressing
{
    [Serializable]
    public class NodeAddress : InstanceAddress
    {
        public NodeAddress(string address)
            : base(AddressTypes.Node, address)
        {
        }
    }
}
