using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    public class NECx : Tuple<int, int>, IoStorm.Payload.IIRProtocol
    {
        public NECx(int address, int command, bool repeat = false)
            : base(address & 0xffff, command & 0xffff)
        {
            Repeat = repeat;
        }

        public int Address { get { return Item1; } }

        public int Command { get { return Item2; } }

        public bool Repeat { get; private set; }

        public override string ToString()
        {
            return string.Format("A{0:x} C{1:x}{2}", Address, Command, Repeat ? " R" : "");
        }
    }
}
