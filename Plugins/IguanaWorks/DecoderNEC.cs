using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.Plugins.IguanaWorks
{
    public class DecoderNEC : DecoderBase
    {
        private const int NEC_RPT_SPACE = 2250;
        private IoStorm.IRProtocol.NEC lastValue;

        public DecoderNEC(ILog log, Action<Payload.IIRProtocol> receivedCommand)
            : base(log, receivedCommand)
        {
        }

        public override bool Decode(IrData irData)
        {
            // Check for repeat
            if (irData.Data.Count == 4 &&
                DecoderHelper.MATCH(irData.Data[2], NEC_RPT_SPACE) &&
                DecoderHelper.MATCH(irData.Data[3], 564))
            {
                // Repeat

                if (this.lastValue == null)
                    // We don't know what to repeat
                    return false;

                this.receivedCommand(new IoStorm.IRProtocol.NEC(this.lastValue.AddressH, this.lastValue.AddressL, this.lastValue.Command, true));

                return true;
            }

            BitBuilder bits;
            if (!DecodeGeneric(irData, out bits, 68, 564 * 16, 564 * 8, 0, 564, 564 * 3, 564))
                return false;

            byte addressL = bits.GetByteMSB(0);
            byte addressH = bits.GetByteMSB(8);
            byte command = bits.GetByteMSB(16);
            byte commandI = bits.GetByteMSB(24);

            if (command != (byte)~commandI)
                // Should be inverted, invalid
                return false;

            var protocol = new IoStorm.IRProtocol.NECx(addressH, addressL, command);
            this.lastValue = protocol;

            this.receivedCommand(protocol);

            return true;
        }

        public override IrData Encode(Payload.IIRProtocol input)
        {
            var value = input as IoStorm.IRProtocol.NEC;
            if (value == null)
                return null;

            // TODO
            return null;
        }
    }
}
