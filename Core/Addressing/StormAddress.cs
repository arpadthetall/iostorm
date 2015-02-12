using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IoStorm.Addressing
{
    [Serializable]
    [JsonConverter(typeof(JsonAddressConverter))]
    public abstract class StormAddress
    {
        public enum AddressTypes
        {
            Unknown,
            Plugin,
            Zone,
            Node,
            Hub
        }

        public string DebugInfo { get; set; }

        public AddressTypes AddressType { get; private set; }

        public string Address { get; private set; }

        public StormAddress(AddressTypes addressType, string address)
        {
            AddressType = addressType;
            Address = address;
        }

        public override string ToString()
        {
            return string.Format("{0}:{1} ({2})", AddressType, Address, DebugInfo);
        }

        public override bool Equals(object obj)
        {
            var b = (StormAddress)obj;
            return b.AddressType == AddressType && b.Address == Address;
        }

        public override int GetHashCode()
        {
            return AddressType.GetHashCode() + Address.GetHashCode();
        }

        public string Id
        {
            get { return string.Format("{0}:{1}", AddressType, Address); }
        }
    }
}
