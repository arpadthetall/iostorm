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
        private long lastValue;

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
                this.receivedCommand(new IoStorm.IRProtocol.NEC2((int)this.lastValue >> 16, (int)this.lastValue, true));

                return true;
            }

            long value;
            if (!DecodeGeneric(irData, out value, 68, 564 * 16, 564 * 8, 0, 564, 564 * 3, 564))
                return false;

            this.lastValue = value;

            this.receivedCommand(new IoStorm.IRProtocol.NEC2((int)value >> 16, (int)value));

            return true;
        }

        public override IrData Encode(Payload.IIRProtocol input)
        {
            var value = input as IoStorm.IRProtocol.NECx;
            if (value == null)
                return null;

            // TODO
            return null;
        }
    }
}
