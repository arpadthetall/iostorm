using System;
using System.Collections.Generic;
using System.Text;

namespace ZWaveApi.Net.Utilities {
    public static class Extensions {

        /// <summary>
        /// Get the array slice between the two indexes.
        /// ... Inclusive for start index, exclusive for end index.
        /// </summary>  0, 1, 2, 3, 4
        ///             2, 3 = 3-2 = 1
        ///             2, 5 = 
        /// 
        public static byte[] ArraySlice(byte[] source, int start, int end) {
            byte[] destfoo = new byte[end - start + 1];
            Array.Copy(source, start, destfoo, 0, end- start + 1);
            
            return destfoo;
        }

        public static byte[] ArraySlice(byte[] source, int start) {
            return ArraySlice(source, start, source.Length - 1);
        }
    }

}
