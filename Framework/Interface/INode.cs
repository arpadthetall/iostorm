﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;

namespace IoStorm
{
    public interface INode
    {
        IoStorm.Addressing.NodeAddress InstanceId { get; }
    }
}
