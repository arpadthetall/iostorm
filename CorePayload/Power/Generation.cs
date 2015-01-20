using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Power
{
    public class Generation : BasePayload
    {
        public GenerationTypes GenerationType { get; set; }

        public long WattsNow { get; set; }

        public override string GetDebugInfo()
        {
            return string.Format("Current power generation: {0} W", WattsNow);
        }
    }
}
