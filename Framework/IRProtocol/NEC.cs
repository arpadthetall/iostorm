using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    // Reference: http://www.sbprojects.com/knowledge/ir/nec.php
    // Reference: http://wiki.slimdevices.com/index.php/Remote_IR_codes

    public class NEC : Tuple<byte, byte, byte>, IoStorm.Payload.IIRProtocol
    {
        public NEC(int addressH, int addressL, int command, bool repeat = false)
            : base((byte)addressH, (byte)addressL, (byte)command)
        {
            Repeat = repeat;
        }

        public byte AddressH { get { return Item1; } }

        public byte AddressL { get { return Item2; } }

        public byte Command { get { return Item3; } }

        public bool Repeat { get; private set; }

        public override string ToString()
        {
            return string.Format("A{0}.{1} OBC {2}{3}", AddressH, AddressL, Command, Repeat ? " R" : "");
        }
    }
}
