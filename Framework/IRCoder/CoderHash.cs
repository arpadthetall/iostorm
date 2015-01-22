using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;

namespace IoStorm.IRCoder
{
    public class CoderHash : CoderBase
    {
        // Use FNV hash algorithm: http://isthe.com/chongo/tech/comp/fnv/#FNV-param
        private const uint FNV_PRIME_32 = 16777619;
        private const uint FNV_BASIS_32 = 2166136261;

        public CoderHash(ILog log, Action<Payload.IIRProtocol> receivedCommand)
            : base(log, receivedCommand)
        {
        }

        // Compare two tick values, returning 0 if newval is shorter,
        // 1 if newval is equal, and 2 if newval is longer
        // Use a tolerance of 20%
        private int Compare(int oldval, int newval)
        {
            if (newval < oldval * .8)
            {
                return 0;
            }
            else if (oldval < newval * .8)
            {
                return 2;
            }
            else
            {
                return 1;
            }
        }

        public override bool Decode(IrData irData)
        {
            // Require at least 6 samples to prevent triggering on noise
            if (irData.Data.Count < 6)
            {
                return false;
            }

            long hash = FNV_BASIS_32;
            for (int i = 1; i + 2 < irData.Data.Count; i++)
            {
                int value = Compare(irData.Data[i], irData.Data[i + 2]);

                // Add value into the hash
                hash = (hash * FNV_PRIME_32) ^ value;
            }

            this.receivedCommand(new IoStorm.IRProtocol.Hash(hash.ToString("x")));

            return true;
        }

        public override IrData Encode(Payload.IIRProtocol input)
        {
            // Impossible to encode from hash
            return null;
        }
    }
}
