using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.RemoteMapping.IRProtocol
{
    // Reference: http://www.sbprojects.com/knowledge/ir/sirc.php

    public class Sony12 : Storm.Payload.IIRProtocol
    {
        private int address;
        private int command;

        public Sony12(int address, int command)
        {
            this.address = address;
            this.command = command;
        }

        public override string ToString()
        {
            return string.Format("A{0} C{1}", this.address, this.command);
        }
    }
}
