using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.IRCoder
{
    public static class CoderHelper
    {
        private const int USECPERTICK = 50;  // microseconds per clock interrupt tick
        private const int RAWBUF = 100; // Length of raw duration buffer

        // Marks tend to be 100us too long, and spaces 100us too short
        // when received due to sensor lag.
        private const int MARK_EXCESS = 100;

        private const double TOLERANCE = 25;

        //#define TOLERANCE 25  // percent tolerance in measurements
        //#define LTOL (1.0 - TOLERANCE/100.) 
        //#define UTOL (1.0 + TOLERANCE/100.) 

        //        #define TICKS_LOW(us) (int) (((us)*LTOL/USECPERTICK))
        //#define TICKS_HIGH(us) (int) (((us)*UTOL/USECPERTICK + 1))

        private static int SubtractTolerance(int value)
        {
            return value - (int)(value * TOLERANCE / 100.0);
        }

        private static int AddTolerance(int value)
        {
            return value + (int)(value * TOLERANCE / 100.0);
        }

        public static bool MATCH(int measured, int desired)
        {
            return measured >= SubtractTolerance(desired) && measured <= AddTolerance(desired);
        }

        public static bool MATCH_MARK(int measured, int desired)
        {
            return MATCH(measured, (desired + MARK_EXCESS));
        }

        public static bool MATCH_SPACE(int measured, int desired)
        {
            return MATCH(measured, (desired - MARK_EXCESS));
        }

        public static bool ABS_MATCH(int measured, int desired, int tolerance)
        {
            return (measured >= (desired - tolerance) && measured <= (desired + tolerance));
        }

        /*
         * Again we use a generic routine because most protocols have the same basic structure. However we need to
         * indicate whether or not the protocol varies the length of the mark or the space to indicate a "0" or "1".
         * If "Mark_One" is zero. We assume that the length of the space varies. If "Mark_One" is not zero then
         * we assume that the length of Mark varies and the value passed as "Space_Zero" is ignored.
         * When using variable length Mark, assumes Head_Space==Space_One. If it doesn't, you need a specialized decoder.
         */
        /*public static bool decodeGeneric(int Raw_Count, int Head_Mark, int Head_Space, 
                                         int Mark_One, int Mark_Zero, int Space_One, int Space_Zero) {
        // If raw samples count or head mark are zero then don't perform these tests.
        // Some protocols need to do custom header work.
          long data = 0;  int Max; offset=1;
          if (Raw_Count > 0) {if (rawlen != Raw_Count) return RAW_COUNT_ERROR;}
          if(!IgnoreHeader) {
            if (Head_Mark) {
              if (!MATCH(rawbuf[offset],Head_Mark)) return HEADER_MARK_ERROR(Head_Mark);
            }
          }
          offset++;
          if (Head_Space) {if (!MATCH(rawbuf[offset],Head_Space)) return HEADER_SPACE_ERROR(Head_Space);}

          if (Mark_One) {//Length of a mark indicates data "0" or "1". Space_Zero is ignored.
            offset=2;//skip initial gap plus header Mark.
            Max=rawlen;
            while (offset < Max) {
              if (!MATCH(rawbuf[offset], Space_One)) return DATA_SPACE_ERROR(Space_One);
              offset++;
              if (MATCH(rawbuf[offset], Mark_One)) {
                data = (data << 1) | 1;
              } 
              else if (MATCH(rawbuf[offset], Mark_Zero)) {
                data <<= 1;
              } 
              else return DATA_MARK_ERROR(Mark_Zero);
              offset++;
            }
            bits = (offset - 1) / 2;
          }
          else {//Mark_One was 0 therefore length of a space indicates data "0" or "1".
            Max=rawlen-1; //ignore stop bit
            offset=3;//skip initial gap plus two header items
            while (offset < Max) {
              if (!MATCH (rawbuf[offset],Mark_Zero)) return DATA_MARK_ERROR(Mark_Zero);
              offset++;
              if (MATCH(rawbuf[offset],Space_One)) {
                data = (data << 1) | 1;
              } 
              else if (MATCH (rawbuf[offset],Space_Zero)) {
                data <<= 1;
              } 
              else return DATA_SPACE_ERROR(Space_Zero);
              offset++;
            }
            bits = (offset - 1) / 2 -1;//didn't encode stop bit
          }
          // Success
          value = data;
          return true;
        }*/
    }
}
