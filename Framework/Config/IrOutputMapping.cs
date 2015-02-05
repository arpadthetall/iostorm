using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Config
{
    public class IrConfig
    {
        public string Make { get; set; }

        public string Models { get; set; }

        public int PowerOnDelayMs { get; set; }

        public List<IrOutputMapping> IrMapping { get; set; }
    }

    public class IrOutputMapping
    {
        public string Payload { get; set; }

        public Dictionary<string, string> Match { get; set; }

        public IrOutputTransmit Transmit { get; set; }
    }

    public class IrOutputTransmit
    {
        public int Repeat { get; set; }

        public string Protocol { get; set; }

        public int Address { get; set; }

        public int AddressH { get; set; }

        public int AddressL { get; set; }

        public int Command { get; set; }

        public int Extended { get; set; }
    }
}
