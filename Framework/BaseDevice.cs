﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm
{
    public abstract class BaseDevice : IDevice
    {
        public string InstanceId { get; set; }
    }
}
