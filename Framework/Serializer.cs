using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Storm
{
    public class Serializer
    {
        public string SerializeObject(object obj)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                TypeNameHandling = TypeNameHandling.All
            };

            return JsonConvert.SerializeObject(obj, settings);
        }

        public object DeserializeString(string input)
        {
            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                TypeNameHandling = TypeNameHandling.All
            };

            return JsonConvert.DeserializeObject(input, settings);
        }
    }
}
