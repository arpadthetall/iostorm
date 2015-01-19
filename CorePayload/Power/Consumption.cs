using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Power
{
    public class Consumption : BasePayload
    {
        public long WattsNow { get; set; }
    }
}
