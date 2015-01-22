using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{

    public class NECx : NEC
    {
        public NECx(int addressH, int addressL, int command, bool repeat = false)
            : base(addressH, addressL, command, repeat)
        {
        }
    }
}
