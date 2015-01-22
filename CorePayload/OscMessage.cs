using System;
using System.Collections.Generic;

namespace IoStorm.Payload
{
    public class OscMessage : BasePayload
    {
        public string Address { get; set; }

        public string Value { get; set; }

        public override string GetDebugInfo()
        {
            return string.Format("OSC {0} = {1}", Address, Value);
        }
    }
}
