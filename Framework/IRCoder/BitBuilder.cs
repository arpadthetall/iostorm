using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.IRCoder
{
    public class BitBuilder
    {
        private List<bool> bits;

        public BitBuilder(int capacity = 0)
        {
            this.bits = new List<bool>(capacity);
        }

        public void AddByteLSB(byte value, int count = 8)
        {
            bool[] newBits = new bool[8];

            newBits[0] = (value & 0x01) != 0;
            newBits[1] = (value & 0x02) != 0;
            newBits[2] = (value & 0x04) != 0;
            newBits[3] = (value & 0x08) != 0;
            newBits[4] = (value & 0x10) != 0;
            newBits[5] = (value & 0x20) != 0;
            newBits[6] = (value & 0x40) != 0;
            newBits[7] = (value & 0x80) != 0;

            this.bits.AddRange(newBits.Take(count));
        }

        public void AddByteMSB(byte value)
        {
            bool[] newBits = new bool[8];

            newBits[0] = (value & 0x80) != 0;
            newBits[1] = (value & 0x40) != 0;
            newBits[2] = (value & 0x20) != 0;
            newBits[3] = (value & 0x10) != 0;
            newBits[4] = (value & 0x08) != 0;
            newBits[5] = (value & 0x04) != 0;
            newBits[6] = (value & 0x02) != 0;
            newBits[7] = (value & 0x01) != 0;

            this.bits.AddRange(newBits);
        }

        public bool Get(int index)
        {
            if (index < 0 || index >= this.bits.Count)
                throw new ArgumentOutOfRangeException("Index");

            return this.bits[index];
        }

        public void AddBit(bool bit)
        {
            this.bits.Add(bit);
        }

        public int Count
        {
            get { return this.bits.Count; }
        }
/*
        public byte GetByteLSB(int index)
        {
            if (index < 0 || index + 8 > this.bits.Count)
                throw new ArgumentOutOfRangeException("Index");

            byte b = (byte)(
                (this.bits[index + 0] ? 0x01 : 0) |
                (this.bits[index + 1] ? 0x02 : 0) |
                (this.bits[index + 2] ? 0x04 : 0) |
                (this.bits[index + 3] ? 0x08 : 0) |
                (this.bits[index + 4] ? 0x10 : 0) |
                (this.bits[index + 5] ? 0x20 : 0) |
                (this.bits[index + 6] ? 0x40 : 0) |
                (this.bits[index + 7] ? 0x80 : 0));

            return b;
        }
*/
        public byte GetByteLSB(int index, int count = 8)
        {
            if (index < 0 || index + count > this.bits.Count || count <= 0 || count > 8)
                throw new ArgumentOutOfRangeException("Index/Count");

            byte b = (byte)(this.bits[index] ? 0x01 : 0);
            b |= (byte)(count >= 2 && this.bits[index + 1] ? 0x02 : 0);
            b |= (byte)(count >= 3 && this.bits[index + 2] ? 0x04 : 0);
            b |= (byte)(count >= 4 && this.bits[index + 3] ? 0x08 : 0);
            b |= (byte)(count >= 5 && this.bits[index + 4] ? 0x10 : 0);
            b |= (byte)(count >= 6 && this.bits[index + 5] ? 0x20 : 0);
            b |= (byte)(count >= 7 && this.bits[index + 6] ? 0x40 : 0);
            b |= (byte)(count >= 8 && this.bits[index + 7] ? 0x80 : 0);

            return b;
        }

        public byte GetByteMSB(int index)
        {
            if (index < 0 || index + 8 > this.bits.Count)
                throw new ArgumentOutOfRangeException("Index");

            byte b = (byte)(
                (this.bits[index + 0] ? 0x80 : 0) |
                (this.bits[index + 1] ? 0x40 : 0) |
                (this.bits[index + 2] ? 0x20 : 0) |
                (this.bits[index + 3] ? 0x10 : 0) |
                (this.bits[index + 4] ? 0x08 : 0) |
                (this.bits[index + 5] ? 0x04 : 0) |
                (this.bits[index + 6] ? 0x02 : 0) |
                (this.bits[index + 7] ? 0x01 : 0));

            return b;
        }
    }
}
