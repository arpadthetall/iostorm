using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IoStorm.Plugins.OscServer.Config
{
    public class Sendpayload
    {
        public string Destination { get; set; }

        public string Payload { get; set; }

        public JObject Parameters { get; set; }
    }
}
