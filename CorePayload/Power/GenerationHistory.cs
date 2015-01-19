using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Power
{
    public class GenerationHistory : Generation
    {
        public long WattHoursToday { get; set; }

        public long WattHoursSevenDays { get; set; }

        public long WattHoursLifetime { get; set; }
    }
}
