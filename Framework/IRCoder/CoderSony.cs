//#define VERBOSE_IR_DATA

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.IRCoder
{
    public class CoderSony : CoderBase
    {
        private IRProtocol.SonyBase lastCommand;
        private DateTime lastTimestamp;
        private int repeatCounter;

        public CoderSony(ILog log, Action<Payload.IIRProtocol> receivedCommand)
            : base(log, receivedCommand)
        {
        }

        public override bool Decode(IrData irData)
        {
            int irLen = irData.Data.Count;
            if (/*irLen != 2 * 8 + 2 &&*/
                irLen != 2 * 12 + 2 &&
                irLen != 2 * 15 + 2 &&
                irLen != 2 * 20 + 2)
                return false;

            BitBuilder bits;
            if (!DecodeGeneric(irData, out bits, 0, 600 * 4, 600, 600 * 2, 600, 600, 0))
                return false;

            byte command = bits.GetByteLSB(0, 7);
            byte address = bits.GetByteLSB(7, bits.Count == 15 ? 8 : 5);
            byte extended = 0;
            if (bits.Count == 20)
                extended = bits.GetByteLSB(12);

            IRProtocol.SonyBase irCommand;
            switch (bits.Count)
            {
                case 12:
                    irCommand = new IRProtocol.Sony12(address, command);
                    break;

                case 15:
                    irCommand = new IRProtocol.Sony15(address, command);
                    break;

                case 20:
                    irCommand = new IRProtocol.Sony20(address, command, extended);
                    break;

                default:
                    return false;
            }

            DateTime now = DateTime.Now;
            if (this.lastCommand != null && this.lastCommand.Equals(irCommand))
            {
                // Check for repeats
                if ((now - this.lastTimestamp).TotalMilliseconds < 90 && this.repeatCounter < 2)
                {
                    // Repeat
                    this.repeatCounter++;

#if VERBOSE_IR_DATA

                    this.log.Trace("Repeats {0} within {1:N0} ms, ignore", this.repeatCounter, (now - this.lastTimestamp).TotalMilliseconds);
#endif
                    lastTimestamp = now;

                    return true;
                }
            }
            this.lastCommand = irCommand;
            this.lastTimestamp = now;
            this.repeatCounter = 0;

            this.receivedCommand(irCommand);

            return true;
        }

        public override IrData Encode(IoStorm.Payload.IIRProtocol input)
        {
            var value = input as IoStorm.IRProtocol.SonyBase;
            if (value == null)
                return null;

            var bitBuilder = new BitBuilder();
            bitBuilder.AddByteLSB(value.Command, 7);

            if (value is IoStorm.IRProtocol.Sony12)
            {
                bitBuilder.AddByteLSB(value.Address, 5);
            }
            else if (value is IoStorm.IRProtocol.Sony15)
            {
                bitBuilder.AddByteLSB(value.Address, 8);
            }
            else if (value is IoStorm.IRProtocol.Sony20)
            {
                bitBuilder.AddByteLSB(value.Address, 5);
                bitBuilder.AddByteLSB(((IoStorm.IRProtocol.Sony20)value).Extended, 8);
            }

            int bits = bitBuilder.Count;

            var result = BuildGeneric(bitBuilder,
                bits, 600 * 4, 600, 600 * 2, 600, 600, 600, 40000, false, (bits == 8) ? 22000 : 45000);

            result.Repeater = 3;

            return result;
        }
    }
}
