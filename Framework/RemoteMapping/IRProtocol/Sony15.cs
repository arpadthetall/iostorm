using System;
using System.Collections.Generic;

namespace IoStorm.RemoteMapping.IRProtocol
{
    public class Sony15 : BaseSony
    {
        public Sony15(int address, int command)
            : base(address, command)
        {
        }
    }
}
