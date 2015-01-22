using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.IRCoder
{
    public class CoderNECx : CoderBase
    {
        public CoderNECx(ILog log, Action<Payload.IIRProtocol> receivedCommand)
            : base(log, receivedCommand)
        {
        }

        public override bool Decode(IrData irData)
        {
            BitBuilder bits;
            if (!DecodeGeneric(irData, out bits, 68, 564 * 8, 564 * 8, 0, 564, 564 * 3, 564))
                return false;

            byte addressL = bits.GetByteLSB(0);
            byte addressH = bits.GetByteLSB(8);
            byte command = bits.GetByteLSB(16);
            byte commandI = bits.GetByteLSB(24);

            if (command != (byte)~commandI)
                // Should be inverted, invalid
                return false;

            this.receivedCommand(new IoStorm.IRProtocol.NECx(addressH, addressL, command));

            return true;
        }

        public override IrData Encode(IoStorm.Payload.IIRProtocol input)
        {
            var value = input as IoStorm.IRProtocol.NECx;
            if (value == null)
                return null;

            var bitBuilder = new BitBuilder(32);
            bitBuilder.AddByteLSB(value.AddressH);
            bitBuilder.AddByteLSB(value.AddressL);
            bitBuilder.AddByteLSB(value.Command);
            bitBuilder.AddByteLSB((byte)~value.Command);

            var result = BuildGeneric(bitBuilder,
                32, 564 * 8, 564 * 8, 564, 564, 564 * 3, 564, 38000, true, 108000);

            return result;
        }
    }
}
