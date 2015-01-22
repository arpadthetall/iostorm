using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.Plugins.IguanaWorks
{
    public class DecoderNECx : DecoderBase
    {
        public DecoderNECx(ILog log, Action<Payload.IIRProtocol> receivedCommand)
            : base(log, receivedCommand)
        {
        }

        public override bool Decode(IrData irData)
        {
            long value;
            if (!DecodeGeneric(irData, out value, 68, 564 * 8, 564 * 8, 0, 564, 564 * 3, 564))
                return false;

            this.receivedCommand(new IoStorm.IRProtocol.NECx((int)(value >> 16), (int)value));

            return true;
        }

        public override IrData Encode(IoStorm.Payload.IIRProtocol input)
        {
            var value = input as IoStorm.IRProtocol.NECx;
            if (value == null)
                return null;

            var result = BuildGeneric(((long)value.Address << 16) + value.Command,
                32, 564 * 8, 564 * 8, 564, 564, 564 * 3, 564, 38000, true, 108000);

            return result;
        }
    }
}
