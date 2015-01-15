using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    // Reference: http://www.sbprojects.com/knowledge/ir/sirc.php

    public abstract class BaseSony : Tuple<int, int>, IoStorm.Payload.IIRProtocol
    {
        public BaseSony(int address, int command)
            : base(address, command)
        {
        }

        public int Address { get { return Item1; } }

        public int Command { get { return Item2; } }

        public override string ToString()
        {
            return string.Format("A{0} C{1}", Address, Command);
        }
    }
}
