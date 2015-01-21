#define VERBOSE_IR_DATA
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.Plugins.IguanaWorks
{
    public abstract class DecoderBase
    {
        protected ILog log;
        protected Action<Payload.IIRProtocol> receivedCommand;

        public DecoderBase(ILog log, Action<Payload.IIRProtocol> receivedCommand)
        {
            this.log = log;
            this.receivedCommand = receivedCommand;
        }

        public abstract bool Decode(IrData irData);

        //protected long value;           // Decoded value
        //protected int bits;            // Number of bits in decoded value
        //protected int[] irData.Data; // Raw intervals in microseconds
        //protected char rawlen;          // Number of records in irData.Data.
        //protected bool IgnoreHeader;             // Relaxed header detection allows AGC to settle
        //protected int offset;

        private void TraceError(string errorType, int offset, int value, int expected)
        {
#if VERBOSE_IR_DATA
            this.log.Trace("Invalid {0} (Idx: {1}   Val: {2}   Exp: {3})", errorType, offset, value, expected);
#endif
        }

        /*
         * Again we use a generic routine because most protocols have the same basic structure. However we need to
         * indicate whether or not the protocol varies the length of the mark or the space to indicate a "0" or "1".
         * If "Mark_One" is zero. We assume that the length of the space varies. If "Mark_One" is not zero then
         * we assume that the length of Mark varies and the value passed as "Space_Zero" is ignored.
         * When using variable length Mark, assumes Head_Space==Space_One. If it doesn't, you need a specialized decoder.
         */
        protected bool DecodeGeneric(
            IrData irData,
            out long output,
            int expectedCount,
            int headMarkLen,
            int headSpaceLen,
            int markOneLen,
            int markZeroLen,
            int spaceOneLen,
            int spaceZeroLen,
            bool ignoreHeader = false)
        {
            // If raw samples count or head mark are zero then don't perform these tests.
            // Some protocols need to do custom header work.
            output = 0;
            long data = 0;
            int Max;
            int offset = 1;
            int rawlen = irData.Data.Count;
            int bits = 0;

            if (expectedCount != 0)
            {
                if (rawlen != expectedCount)
                {
#if VERBOSE_IR_DATA
                    this.log.Trace("Invalid Number of raw samples");
#endif
                    return false;
                }
            }
            if (!ignoreHeader)
            {
                if (headMarkLen != 0)
                {
                    if (!DecoderHelper.MATCH(irData.Data[offset], headMarkLen))
                    {
                        TraceError("Header Mark", offset, irData.Data[offset], headMarkLen);
                        return false;
                    }
                }
            }
            offset++;
            if (headSpaceLen != 0)
            {
                if (!DecoderHelper.MATCH(irData.Data[offset], headSpaceLen))
                {
                    TraceError("Header Space", offset, irData.Data[offset], headSpaceLen);
                    return false;
                }
            }

            if (markOneLen != 0)
            {
                //Length of a mark indicates data "0" or "1". Space_Zero is ignored.
                offset = 2;//skip initial gap plus header Mark.
                Max = rawlen;
                while (offset < Max)
                {
                    if (!DecoderHelper.MATCH(irData.Data[offset], spaceOneLen))
                    {
                        TraceError("Data Space", offset, irData.Data[offset], spaceOneLen);
                        return false;
                    }
                    offset++;
                    if (DecoderHelper.MATCH(irData.Data[offset], markOneLen))
                    {
                        data = (data << 1) | 1;
                    }
                    else if (DecoderHelper.MATCH(irData.Data[offset], markZeroLen))
                    {
                        data <<= 1;
                    }
                    else
                    {
                        TraceError("Data Mark", offset, irData.Data[offset], markZeroLen);
                        return false;
                    }
                    offset++;
                }
                bits = (offset - 1) / 2;
            }
            else
            {
                //Mark_One was 0 therefore length of a space indicates data "0" or "1".
                Max = rawlen - 1; //ignore stop bit
                offset = 3;//skip initial gap plus two header items
                while (offset < Max)
                {
                    if (!DecoderHelper.MATCH(irData.Data[offset], markZeroLen))
                    {
                        TraceError("Data Mark", offset, irData.Data[offset], markZeroLen);
                        return false;
                    }
                    offset++;
                    if (DecoderHelper.MATCH(irData.Data[offset], spaceOneLen))
                    {
                        data = (data << 1) | 1;
                    }
                    else if (DecoderHelper.MATCH(irData.Data[offset], spaceZeroLen))
                    {
                        data <<= 1;
                    }
                    else
                    {
                        TraceError("Data Space", offset, irData.Data[offset], spaceZeroLen);
                        return false;
                    }
                    offset++;
                }
                bits = (offset - 1) / 2 - 1;    //didn't encode stop bit
            }

            // Success
            output = data;
            return true;
        }
    }
}
