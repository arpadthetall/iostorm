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

            this.receivedCommand(new IoStorm.IRProtocol.NEC2((Int16)(value >> 16), (Int16)value));

            return true;
        }
    }
}
