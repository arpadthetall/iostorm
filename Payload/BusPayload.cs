﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class BusPayload
    {
        public string OriginDeviceId { get; set; }

        public IPayload Payload { get; set; }
    }
}
