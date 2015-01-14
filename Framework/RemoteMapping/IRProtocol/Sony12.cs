using System;
using System.Collections.Generic;

namespace IoStorm.RemoteMapping.IRProtocol
{
    public class Sony12 : BaseSony
    {
        public Sony12(int address, int command)
            : base(address, command)
        {
        }
    }
}
