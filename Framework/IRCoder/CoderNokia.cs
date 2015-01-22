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
    public class CoderNokia : CoderBase
    {
        private const int RCMM_HEAD_MARK = 417;
        private const int RCMM_DATA_MARK = 167;
        private const int RCMM_ZERO = 278;
        private const int RCMM_ONE = 444;
        private const int RCMM_TWO = 611;
        private const int RCMM_THREE = 778;
        private const int RCMM_TOLERANCE = 80;

        public CoderNokia(ILog log, Action<Payload.IIRProtocol> receivedCommand)
            : base(log, receivedCommand)
        {
        }

        /*
        * According to http://www.hifi-remote.com/johnsfine/DecodeIR.html#Nokia
        * The IRP notation for these protocols are:
        * Nokia 12 bit: {36k,msb}<164,-276|164,-445|164,-614|164,-783>(412,-276,D:4,F:8,164,-???)+ 
        * Nokia 24-bit: {36k,msb}<164,-276|164,-445|164,-614|164,-783>(412,-276,D:8,S:8,F:8,164,-???)+ 
        * Nokia 32 bit: {36k,msb}<164,-276|164,-445|164,-614|164,-783>(412,-276,D:8,S:8,X:8,F:8,164,^100m)+ 
        * Slightly different timing values are documented at 
        * http://www.sbprojects.com/knowledge/ir/rcmm.php
        * We will use the timing from the latter reference.
        * Unlike most protocols which defined sequences for a logical "0" and "1", this protocol
        * encodes 2 bits per pulse. Therefore it encodes a logical "2" and "3" as well.
        * The length of the mark is constant but the length of the space denotes the bit values.
        * Note the 32-bit version uses a toggle bit of 0x8000 and as usual it is up to the end-user
        * to implement it outside the library routines.
        */
        /*
        * Normally IRLib uses a plus or minus percentage to determine if an interval matches the
        * desired value. However this protocol uses extremely long intervals of similar length.
        * For example using the default 25% tolerance the RCMM_TWO value 611 would be accepted for 
        * anything between 458 and 763. The low end is actually closer to RCMM_ONE value of 444
        * and the upper range is closer to RCM_THREE value of 778. To implement this protocol
        * we created a new match routine ABS_MATCH which allows you to specify an absolute
        * number of microseconds of tolerance for comparison.
        */

        public override bool Decode(IrData irData)
        {
            int irLen = irData.Data.Count;

            if ((irLen != (12 + 2)) &&
                (irLen != (24 + 2)) &&
                (irLen != (32 + 4)))
            {
#if VERBOSE_IR_DATA
                this.log.Trace("[{0}] Invalid Number of raw samples", this.GetType().Name);
#endif
                return false;
            }

            var bits = new BitBuilder();

            if (!CoderHelper.MATCH(irData.Data[1], RCMM_HEAD_MARK))
            {
                TraceError("Header Mark", 1, irData.Data[1], RCMM_HEAD_MARK);
                return false;
            }

            if (!CoderHelper.MATCH(irData.Data[2], RCMM_ZERO))
            {
                TraceError("Header Space", 2, irData.Data[2], RCMM_ZERO);
                return false;
            }

            int offset = 3;
            while (offset < (irLen - 1))
            {
                if (!CoderHelper.ABS_MATCH(irData.Data[offset], RCMM_DATA_MARK, RCMM_TOLERANCE + 50))
                {
                    TraceError("Data Mark", offset, irData.Data[offset], RCMM_DATA_MARK);
                    return false;
                }

                offset++;
                if (CoderHelper.ABS_MATCH(irData.Data[offset], RCMM_ZERO, RCMM_TOLERANCE))
                {
                    //Logical "0"
                    bits.AddBit(false);
                    bits.AddBit(false);
                }
                else if (CoderHelper.ABS_MATCH(irData.Data[offset], RCMM_ONE, RCMM_TOLERANCE))
                {
                    //Logical "1"
                    bits.AddBit(false);
                    bits.AddBit(true);
                }
                else if (CoderHelper.ABS_MATCH(irData.Data[offset], RCMM_TWO, RCMM_TOLERANCE))
                {
                    //Logical "2"
                    bits.AddBit(true);
                    bits.AddBit(false);
                }
                else if (CoderHelper.ABS_MATCH(irData.Data[offset], RCMM_THREE, RCMM_TOLERANCE))
                {
                    //Logical "3"
                    bits.AddBit(true);
                    bits.AddBit(true);
                }
                else
                {
                    TraceError("Data Space", offset, irData.Data[offset], RCMM_ZERO);
                    return false;
                }
                offset++;
            }
            if (!CoderHelper.MATCH(irData.Data[offset], RCMM_DATA_MARK))
            {
                TraceError("Data Mark", offset, irData.Data[offset], RCMM_DATA_MARK);
                return false;
            }

            switch (bits.Count)
            {
                case 32:
                    this.receivedCommand(new IoStorm.IRProtocol.Nokia32(
                        addressH: bits.GetByteMSB(0),
                        addressL: bits.GetByteMSB(8),
                        extended: bits.GetByteMSB(16),
                        command: bits.GetByteMSB(24),
                        toggle: bits.Get(16)));
                    break;

                default:
                    return false;
            }

            return true;
        }

        public override IrData Encode(IoStorm.Payload.IIRProtocol input)
        {
            var value = input as IoStorm.IRProtocol.Nokia32;
            if (value == null)
                return null;

            var bitBuilder = new BitBuilder();
            bitBuilder.AddByteMSB(value.AddressH);
            bitBuilder.AddByteMSB(value.AddressL);
            bitBuilder.AddByteMSB((byte)((value.Extended & 0x7F) | (value.Toggle ? 0x80 : 0x00)));
            bitBuilder.AddByteMSB(value.Command);

            int bits = bitBuilder.Count;

            var output = new List<Tuple<bool, int>>();

            // Header
            output.Add(Tuple.Create(true, RCMM_HEAD_MARK));
            output.Add(Tuple.Create(false, RCMM_ZERO));

            for (int i = 0; i < bits; i += 2)
            {
                output.Add(Tuple.Create(true, RCMM_DATA_MARK));

                int v = bitBuilder.Get(i) ? 0x02 : 0;
                v |= bitBuilder.Get(i + 1) ? 0x01 : 0;

                switch (v)
                {
                    case 0:
                        output.Add(Tuple.Create(false, RCMM_ZERO));
                        break;

                    case 1:
                        output.Add(Tuple.Create(false, RCMM_ONE));
                        break;

                    case 2:
                        output.Add(Tuple.Create(false, RCMM_TWO));
                        break;

                    case 3:
                        output.Add(Tuple.Create(false, RCMM_THREE));
                        break;
                }
            };

            // Trail
            output.Add(Tuple.Create(true, RCMM_DATA_MARK));
            output.Add(Tuple.Create(false, 27778 - output.Sum(x => x.Item2)));

            var result = BuildIrDataFromTupleList(36000, output);

            return result;
        }
    }
}
