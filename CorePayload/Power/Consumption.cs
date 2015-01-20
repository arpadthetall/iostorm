using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Power
{
    public class Consumption : BasePayload
    {
        public long WattsNow { get; set; }

        public override string GetDebugInfo()
        {
            return string.Format("Current power consumption: {0} W", WattsNow);
        }
    }
}
