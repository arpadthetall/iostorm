using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IoStorm.Config
{
    public class Activity
    {
        public string Name { get; set; }

        public string ZoneId { get; set; }

        public List<JObject> Sequence { get; set; }
    }

    public class ActivitySendPayload
    {
        public string Destination { get; set; }

        public string Payload { get; set; }

        public JObject Parameters { get; set; }
    }

    public class ActivitySleep
    {
        public int Milliseconds { get; set; }
    }
}
