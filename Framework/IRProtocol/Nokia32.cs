using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    // Reference: https://github.com/cyborg5/IRLib/blob/master/examples/rcmm/rcmm.ino
    // Reference: http://www.sbprojects.com/knowledge/ir/rcmm.php

    public class Nokia32 : Tuple<byte, byte, byte, byte>, IoStorm.Payload.IIRProtocol
    {
        public Nokia32(int addressH, int addressL, int command, int extended, bool toggle)
            : base((byte)addressH, (byte)addressL, (byte)command, (byte)(extended & 0x7F))
        {
            Toggle = toggle;
        }

        public byte AddressH { get { return Item1; } }

        public byte AddressL { get { return Item2; } }

        public byte Command { get { return Item3; } }

        public byte Extended { get { return Item4; } }

        public bool Toggle { get; private set; }

        public override string ToString()
        {
            return string.Format("A{0}.{1} OBC {2} X={3}{4}", AddressH, AddressL, Command, Extended, Toggle ? " T" : "");
        }
    }
}
