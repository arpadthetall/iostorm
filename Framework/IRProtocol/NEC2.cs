using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    // Reference: http://www.sbprojects.com/knowledge/ir/nec.php
    // Reference: http://wiki.slimdevices.com/index.php/Remote_IR_codes

    public class NEC2 : Tuple<int, int>, IoStorm.Payload.IIRProtocol
    {
        public NEC2(int address, int command)
            : base(address, command)
        {
        }

        public int Address { get { return Item1; } }

        public int Command { get { return Item2; } }

        public override string ToString()
        {
            return string.Format("A{0:x} C{1:x}", Address, Command);
        }
    }
}
