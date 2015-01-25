using System;
using System.Collections.Generic;
using System.Text;

namespace ZWaveApi.Net.Utilities {


    static class ByteFunctions {

        private const byte _sizeMask = 0x07;
        private const byte _scaleMask = 0x18;
        private const byte _scaleShift = 0x03;
        private const byte _precisionMask = 0xe0;
        private const byte _precisionShift = 0x05;
        
	    static public int ExtractValue(byte[] _data, ref byte _scale) {
		    byte size = (byte)(_data[0] & _sizeMask);
		    byte precision = (byte)((_data[0] & _precisionMask) >> _precisionShift);

            if (_scale != null) {
                _scale = (byte)((_data[0] & _scaleMask) >> _scaleShift);
            }

            UInt32 value = 0;
            int result = 0;
		    byte i ;
            for (i = 0; i < size; ++i) {
                value <<= 8;
                value |= (UInt32)_data[i + 1];
            }


		    // Deal with sign extension.  All values are signed
            if ((_data[1] & 0x80) == 0x80) {

                // MSB is signed
                if (size == 1) {
                    value |= 0xffffff00;
                } else if (size == 2) {
                    value |= 0xffff0000;
                }
                result = (int)(-1 * value);
            } else {
                result = (int)(value);
            }


            if (precision == 0) {
                return result;
            } else {
                return result / (int)Math.Pow(10, precision);
            }
    
	    }


        static public int ByteToInt(byte[] _value) {

            if (BitConverter.IsLittleEndian) {
                Array.Reverse(_value);
            }
            if (_value.Length == 1) {
                return (Byte)(_value[0]);
            } else if (_value.Length == 2) {
                return BitConverter.ToInt16(_value, 0);
            } else {
                return BitConverter.ToInt32(_value, 0);
            }

            /*
             * 
            byte[] _bytes = new byte[4];
            for (int i = 0; i < 4; ++i) {
                if (i < _value.Length) {
                    _bytes[3 - i] = _value[_value.Length - 1 - i];
                } else {
                    _bytes[3-i] = 0;
                }
            }

            string message = "Values : ";

            foreach (byte b in _bytes) {
                message += b.ToString("X2") + " ";
            }
            Console.WriteLine(message);
             * 
            int result = 0;
            for (int i = 0; i < _value.Length; ++i) {
                result <<= 8;
                result |= (int)_value[i];
            }
            return result;
             */
        }

        static public byte[] IntToBytes(int _value) {

            byte[] result;

            int size = 4;

            if (_value < 0) {
                if ((_value & 0xffffff80) == 0xffffff80) {
                    size = 1;
                } else if ((_value & 0xffff8000) == 0xffff8000) {
                    size = 2;
                }
            } else {
                if ((_value & 0xffffff00) == 0) {
                    size = 1;
                } else if ((_value & 0xffff0000) == 0) {
                    size = 2;
                }
            }

            result = new byte[size];

            if (size > 2) {
                result[size - 4] = (byte)(_value >> 24);
                result[size - 3] = (byte)(_value >> 16);
            }
            if (size > 1) {
                result[size - 2] = (byte)(_value >> 8);
            }
            result[size - 1] = (byte)(_value);

            return result;

            /* .Net Way (creating too many bytes)
            * 
            * 
           if (_value <= sbyte.MaxValue && _value >= sbyte.MinValue) {
               result = BitConverter.GetBytes((sbyte)_value);
           } else if (_value <= byte.MaxValue && _value >= byte.MinValue) {
               result = BitConverter.GetBytes((byte)_value);
           } else if (_value <= Int16.MaxValue && _value >= Int16.MinValue) {
               result = BitConverter.GetBytes((Int16)_value);
           } else if (_value <= UInt16.MaxValue && _value >= UInt16.MinValue) {
               result = BitConverter.GetBytes((UInt16)_value);
           } else {
               result = BitConverter.GetBytes(_value);
           }
           if (BitConverter.IsLittleEndian) {
               Array.Reverse(result);
           }
           return result;
            * 
            * 
            **/
        }
    }
}
