using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Power
{
    public class ConsumptionHistory : Consumption
    {
        public long TotalFromGridWattHours { get; set; }

        public long TotalToGridWattHours { get; set; }
    }
}
