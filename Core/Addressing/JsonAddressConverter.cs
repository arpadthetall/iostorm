using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace IoStorm.Addressing
{
    public class JsonAddressConverter : JsonConverter
    {
        internal const string AddressType_Zone = "ZONE";
        internal const string AddressType_Plugin = "PLUG";
        internal const string AddressType_Node = "NODE";
        internal const string AddressType_Hub = "HUB";

        public override bool CanConvert(Type objectType)
        {
            throw new NotImplementedException();
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            object address = GetAddressFromString((string)reader.Value);

            if (address != null)
                return address;

            throw new NotImplementedException();
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            Type objectType = value.GetType();

            if (objectType == typeof(ZoneAddress))
                writer.WriteValue(AddressType_Zone + ":" + (value as ZoneAddress).Address);
            else if (objectType == typeof(NodeAddress))
                writer.WriteValue(AddressType_Node + ":" + (value as NodeAddress).Address);
            else if (objectType == typeof(HubAddress))
                writer.WriteValue(AddressType_Hub + ":" + (value as HubAddress).Address);
            else if (objectType == typeof(PluginAddress))
                writer.WriteValue(AddressType_Plugin + ":" + (value as PluginAddress).Address);
            else
                throw new NotImplementedException();
        }

        internal object GetAddressFromString(string value)
        {
            string[] parts = value.Split(':');

            if (parts.Length != 2)
                throw new ArgumentException("Invalid address string");

            switch (parts[0])
            {
                case AddressType_Node:
                    return new NodeAddress(parts[1]);

                case AddressType_Plugin:
                    return new PluginAddress(parts[1]);

                case AddressType_Zone:
                    return new ZoneAddress(parts[1]);

                case AddressType_Hub:
                    return new HubAddress(parts[1]);
            }


            return null;
        }
    }
}
