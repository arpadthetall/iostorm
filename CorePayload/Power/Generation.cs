using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Power
{
    public class Generation : BasePayload
    {
        public GenerationTypes GenerationType { get; set; }

        public long WattHoursToday { get; set; }

        public long WattHoursSevenDays { get; set; }

        public long WattHoursLifetime { get; set; }

        public long WattsNow { get; set; }
    }
}
