using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    // Reference: http://www.sbprojects.com/knowledge/ir/sirc.php

    public abstract class SonyBase : Tuple<byte, byte, byte>, IoStorm.Payload.IIRProtocol
    {
        public SonyBase(int address, int command)
            : base((byte)address, (byte)command, 0)
        {
        }

        protected SonyBase(int address, int command, int extended)
            : base((byte)address, (byte)command, (byte)extended)
        {
        }

        public byte Address { get { return Item1; } }

        public byte Command { get { return Item2; } }

        public override string ToString()
        {
            return string.Format("A{0} OBC {1}", Address, Command);
        }
    }
}
