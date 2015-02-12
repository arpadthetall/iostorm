using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Addressing
{
    [Serializable]
    public class HubAddress : PluginAddress
    {
        public HubAddress(string address)
            : base(AddressTypes.Hub, address)
        {
        }

        public static HubAddress FromString(string value)
        {
            var converter = new JsonAddressConverter();

            object result = converter.GetAddressFromString(value);

            if (result is HubAddress)
                return (HubAddress)result;

            throw new ArgumentException();
        }
    }
}
