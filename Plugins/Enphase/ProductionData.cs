using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Plugins.Enphase
{
    internal class ProductionData
    {
        public int wattHoursToday { get; set; }

        public int wattHoursSevenDays { get; set; }

        public int wattHoursLifetime { get; set; }

        public int wattsNow { get; set; }
    }
}
