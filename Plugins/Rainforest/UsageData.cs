using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Plugins.Rainforest
{
    internal class UsageData
    {
        public string meter_status { get; set; }

        public double demand { get; set; }

        public string demand_units { get; set; }

        public string demand_timestamp { get; set; }

        public double summation_received { get; set; }

        public double summation_delivered { get; set; }

        public string summation_units { get; set; }

        public double price { get; set; }

        public string price_units { get; set; }

        public string price_label { get; set; }

        public string message_timestamp { get; set; }

        public string message_text { get; set; }

        public string message_confirmed { get; set; }

        public string message_confirm_required { get; set; }

        public string message_id { get; set; }

        public string message_queue { get; set; }

        public string message_priority { get; set; }

        public string message_read { get; set; }
    }
}
