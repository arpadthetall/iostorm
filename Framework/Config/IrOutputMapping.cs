using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Config
{
    public class IrOutputMapping
    {
        public PayloadMatch Match { get; set; }

        public IrOutputTransmit Transmit { get; set; }
    }

    public class PayloadMatch
    {
        // Doesn't have to be Value, should be dynamic
        public string Value { get; set; }
    }

    public class IrOutputTransmit
    {
        public int Repeat { get; set; }

        public string Protocol { get; set; }

        public int Device { get; set; }

        public int Command { get; set; }
    }
}
