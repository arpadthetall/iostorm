﻿using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    // Reference: http://www.sbprojects.com/knowledge/ir/nec.php
    // Reference: http://wiki.slimdevices.com/index.php/Remote_IR_codes

    public class NEC2 : Tuple<int, int>, IoStorm.Payload.IIRProtocol
    {
        public NEC2(int address, int command, bool repeat = false)
            : base(address & 0xffff, command & 0xffff)
        {
            Repeat = repeat;
        }

        public int Address { get { return Item1; } }

        public int Command { get { return Item2; } }

        public bool Repeat { get; private set; }

        public override string ToString()
        {
            return string.Format("A{0:x4} C{1:x4}{2}", Address, Command, Repeat ? " R" : "");
        }
    }
}
