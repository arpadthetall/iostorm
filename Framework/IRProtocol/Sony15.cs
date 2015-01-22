using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    public class Sony15 : SonyBase
    {
        public Sony15(int address, int command)
            : base(address, command)
        {
        }
    }
}
