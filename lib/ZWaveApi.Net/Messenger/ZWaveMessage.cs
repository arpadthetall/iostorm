/* 
 *	Copyright (C) 2010- ZWaveApi
 *	http://ZWaveApi.net
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation; either version 3, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/lesser.html
 *
 */

namespace ZWaveApi.Net.Messenger
{
    /// <summary>
    /// This class define the message that is be send or receive by ZWave.
    /// </summary>
    public class ZWaveMessage
    {
        public ZWaveMessagePriority Priority { get; private set; }
        public byte callbackId { get; private set; }
        public byte[] buffer { get; private set; }


        /// <summary>
        /// Create a message with the Callback ID and Activation
        /// </summary>
        /// <param name="message">Array of bytes including the message</param>
        /// <param name="priority">The priority of the message</param>
        /// <param name="callbackId">The Callback ID</param>
        /// <param name="activation">The activation object</param>
        public ZWaveMessage(byte[] buffer, ZWaveMessagePriority priority, byte callbackId)
        {
            this.Priority = priority;
            this.callbackId = callbackId;
            this.buffer = buffer;
        }

        /// <summary>
        /// Create a message without the Callback ID but with Activation.
        /// </summary>
        /// <param name="message">Array of bytes including the message</param>
        /// <param name="priority">The priority of the message</param>
        public ZWaveMessage(byte[] buffer, ZWaveMessagePriority priority)
            : this(buffer, priority, 0)
        {
        }

        public ZWaveMessage(byte[] buffer)
            : this (buffer, ZWaveMessagePriority.Received, 0)
        {
        }

         /// <summary>
        /// Convert the Byte array in the message to an string
        /// </summary>
        /// <returns>The message as a string</returns>
        public override string ToString()
        {
            string ret = string.Empty;
            foreach (byte b in this.buffer)
            {
                ret += b.ToString("X2") + " ";
            }
            return ret.Trim();
        }
    }
}