using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Addressing
{
    [Serializable]
    public class ZoneAddress : StormAddress
    {
        public ZoneAddress(string address)
            : base(AddressTypes.Zone, address)
        {
        }

        public static ZoneAddress FromString(string value)
        {
            var converter = new JsonAddressConverter();

            object result = converter.GetAddressFromString(value);

            if (result is ZoneAddress)
                return (ZoneAddress)result;

            throw new ArgumentException();
        }
    }
}
