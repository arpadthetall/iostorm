//#define VERBOSE_IR_DATA

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.IRCoder
{
    public abstract class CoderBase
    {
        protected ILog log;
        protected Action<Payload.IIRProtocol> receivedCommand;

        public CoderBase(ILog log, Action<Payload.IIRProtocol> receivedCommand)
        {
            this.log = log;
            this.receivedCommand = receivedCommand;
        }

        public abstract bool Decode(IrData irData);

        public abstract IrData Encode(IoStorm.Payload.IIRProtocol input);

        protected void TraceError(string errorType, int offset, int value, int expected)
        {
#if VERBOSE_IR_DATA
            this.log.Trace("[{0}] Invalid {1} (Idx: {2}   Val: {3}   Exp: {4})", this.GetType().Name, errorType, offset, value, expected);
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
            out BitBuilder output,
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
            output = null;
            int max;
            int offset = 1;
            int rawlen = irData.Data.Count;
            int bits = 0;

            if (expectedCount != 0)
            {
                if (rawlen != expectedCount)
                {
#if VERBOSE_IR_DATA
                    this.log.Trace("[{0}] Invalid Number of raw samples", this.GetType().Name);
#endif
                    return false;
                }
            }
            if (!ignoreHeader)
            {
                if (headMarkLen != 0)
                {
                    if (!CoderHelper.MATCH(irData.Data[offset], headMarkLen))
                    {
                        TraceError("Header Mark", offset, irData.Data[offset], headMarkLen);
                        return false;
                    }
                }
            }
            offset++;
            if (headSpaceLen != 0)
            {
                if (!CoderHelper.MATCH(irData.Data[offset], headSpaceLen))
                {
                    TraceError("Header Space", offset, irData.Data[offset], headSpaceLen);
                    return false;
                }
            }

            output = new BitBuilder();
            if (markOneLen != 0)
            {
                // Length of a mark indicates data "0" or "1". Space_Zero is ignored.
                offset = 2; // skip initial gap plus header Mark.
                max = rawlen;
                while (offset < max)
                {
                    if (!CoderHelper.MATCH(irData.Data[offset], spaceOneLen))
                    {
                        TraceError("Data Space", offset, irData.Data[offset], spaceOneLen);
                        return false;
                    }
                    offset++;
                    if (CoderHelper.MATCH(irData.Data[offset], markOneLen))
                    {
                        output.AddBit(true);
                    }
                    else if (CoderHelper.MATCH(irData.Data[offset], markZeroLen))
                    {
                        output.AddBit(false);
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
                // Mark_One was 0 therefore length of a space indicates data "0" or "1".
                max = rawlen - 1; // ignore stop bit
                offset = 3; // skip initial gap plus two header items
                while (offset < max)
                {
                    if (!CoderHelper.MATCH(irData.Data[offset], markZeroLen))
                    {
                        TraceError("Data Mark", offset, irData.Data[offset], markZeroLen);
                        return false;
                    }
                    offset++;
                    if (CoderHelper.MATCH(irData.Data[offset], spaceOneLen))
                    {
                        output.AddBit(true);
                    }
                    else if (CoderHelper.MATCH(irData.Data[offset], spaceZeroLen))
                    {
                        output.AddBit(false);
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

            if (bits != output.Count)
                // Incorrect number of bits
                return false;

            // Success
            return true;
        }

        protected IrData BuildIrDataFromTupleList(int carrierFrequencyHz, List<Tuple<bool, int>> list)
        {
            var result = new IrData
            {
                FrequencyHertz = carrierFrequencyHz
            };
            bool lastIsPulse = false;
            foreach (var ir in list)
            {
                if (ir.Item1 == lastIsPulse && result.Data.Count > 0)
                {
                    // Add
                    result.Data[result.Data.Count - 1] += ir.Item2;
                }
                else
                {
                    result.Data.Add(ir.Item2);
                }

                lastIsPulse = ir.Item1;
            }

            return result;
        }

        /*
         * Most of the protocols have a header consisting of a mark/space of a particular length followed by 
         * a series of variable length mark/space signals.  Depending on the protocol they very the lengths of the 
         * mark or the space to indicate a data bit of "0" or "1". Most also end with a stop bit of "1".
         * The basic structure of the sending and decoding these protocols led to lots of redundant code. 
         * Therefore I have implemented generic sending and decoding routines. You just need to pass a bunch of customized 
         * parameters and it does the work. This reduces compiled code size with only minor speed degradation. 
         * You may be able to implement additional protocols by simply passing the proper values to these generic routines.
         * The decoding routines do not encode stop bits. So you have to tell this routine whether or not to send one.
         */
        protected IrData BuildGeneric(
            BitBuilder data,
            int numBits,
            int headMarkLen,
            int headSpaceLen,
            int markOneLen,
            int markZeroLen,
            int spaceOneLen,
            int spaceZeroLen,
            int carrierFrequency,
            bool useStopBit,
            int maxExtents)
        {
            var output = new List<Tuple<bool, int>>();

            // Some protocols do not send a header when sending repeat codes. So we pass a zero value to indicate skipping this.
            if (headMarkLen != 0)
                output.Add(Tuple.Create(true, headMarkLen));

            if (headSpaceLen != 0)
                output.Add(Tuple.Create(false, headSpaceLen));

            for (int i = 0; i < numBits; i++)
            {
                if (data.Get(i))
                {
                    output.Add(Tuple.Create(true, markOneLen));
                    output.Add(Tuple.Create(false, spaceOneLen));
                }
                else
                {
                    output.Add(Tuple.Create(true, markZeroLen));
                    output.Add(Tuple.Create(false, spaceZeroLen));
                }
            }

            if (useStopBit)
                output.Add(Tuple.Create(true, markOneLen)); //stop bit of "1"

            if (maxExtents != 0)
            {
                int extents = output.Sum(x => x.Item2);

                output.Add(Tuple.Create(false, maxExtents - extents));
            }
            else
                output.Add(Tuple.Create(false, spaceOneLen));

            // Build IrData
            return BuildIrDataFromTupleList(carrierFrequency, output);
        }
    }
}
