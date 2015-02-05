using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace IoStorm.Plugins.OscServer.Config
{
    public class MatchMessage
    {
        public string Address { get; set; }

        public string MatchValue { get; set; }

        public Sendpayload SendPayload { get; set; }
    }
}
