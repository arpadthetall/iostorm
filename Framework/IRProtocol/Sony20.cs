using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    public class Sony20 : SonyBase
    {
        public Sony20(int address, int command, int extended)
            : base(address, command, extended)
        {
        }

        public byte Extended { get { return Item3; } }

        public override string ToString()
        {
            return string.Format("A{0}x{1} OBC {2}", Address, Extended, Command);
        }
    }
}
