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

using System.Collections.Generic;
using System;

namespace ZWaveApi.Net.Messenger
{
    public class Message
    {
        public FrameHeader frameHeader { get; private set; }
        public MessageType messageType { get; private set; }
        public ZWaveFunction function { get; private set; }
        public byte nodeId { get; private set; }
        public TransmitOptions transmitOptions { get; private set; }
        public byte[] command { get; private set; }
        public byte level { get; private set; }
        public byte callBackId { get; private set; }
        public byte instanceId { get; set; }

        public byte[] buffer;
        public string logText;

        public Message()
        {
        }

        public Message(FrameHeader frameHeader, MessageType messageType, ZWaveFunction function, byte nodeId,
                       byte[] command, TransmitOptions transmitOptions, byte callBackId, byte instanceId)
        {
            this.frameHeader = frameHeader;
            this.messageType = messageType;
            this.function = function;
            this.nodeId = nodeId;
            this.transmitOptions = transmitOptions;
            this.command = command;
            this.level = level;
            this.callBackId = callBackId;
            this.instanceId = 0;
        }
        
        public Message(FrameHeader frameHeader, MessageType messageType, ZWaveFunction function, byte nodeId,
                       byte[] command, TransmitOptions transmitOptions, byte callBackId)
            : this(frameHeader, messageType, function, nodeId, command, transmitOptions, callBackId, 0) {
        }

        public Message(MessageType messageType, ZWaveFunction function, byte nodeId,
                       byte[] command, TransmitOptions transmitOptions, byte callBackId)
            : this(FrameHeader.SOF, messageType, function, nodeId, command, transmitOptions, callBackId)
        {
        }


        public Message(MessageType messageType, ZWaveFunction function, byte nodeId, byte instanceId,
                       byte[] command)
            : this(FrameHeader.SOF, messageType, function, nodeId, command, 0, 0x00, instanceId) {
        }

        public Message(MessageType messageType, ZWaveFunction function, byte nodeId,
                       byte[] command)
            : this(FrameHeader.SOF, messageType, function, nodeId, command, 0, 0x00)
        {
        }

        public Message(MessageType messageType, ZWaveFunction function, byte nodeId)
            : this(FrameHeader.SOF, messageType, function, nodeId, null, 0, 0x00)
        {
        }

        public Message(MessageType messageType, ZWaveFunction function)
            : this(FrameHeader.SOF, messageType, function, 0, null, 0, 0x00)
        {
        }

        public Message(FrameHeader frameHeader)
        {
            this.frameHeader = frameHeader;
            this.instanceId = 0;
        }

        public Message(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }

            this.frameHeader = (FrameHeader)buffer[0];
            
            if (buffer.Length > 2)
                this.messageType = (MessageType)buffer[2];

            if (buffer.Length > 3)
             this.function = (ZWaveFunction)buffer[3];
            
            if (buffer.Length > 5)
                this.nodeId = buffer[5];

            this.buffer = buffer;
            this.instanceId = 0;
        }

        public Message(byte[] buffer, byte instanceId) : this(buffer) {
            this.instanceId = instanceId;
        }

        public byte[] Assembly()
        {
            List<byte> message = new List<byte>();
            message.Add((byte)frameHeader);

            switch (frameHeader)
            {
                case FrameHeader.SOF:
                    message.Add(0x01); // Message length
                    message.Add((byte)this.messageType); // Message type: Request | Response
                    message.Add((byte)this.function); // Function
                    if (this.nodeId != 0) message.Add(this.nodeId); // NodeId if provided
                    if (this.command != null)
                    {

                        message.Add((byte)this.command.Length ); // Command length
                        message.AddRange(command); // Command data
                    }
                    if (this.transmitOptions != 0) message.Add((byte)this.transmitOptions); // Transmit options
                    if (this.callBackId != 0) message.Add(this.callBackId); // CallBackID if provided
                    message.Add(0x00); // Checksum byte. The values will be set later

                    this.buffer = message.ToArray();
                    buffer[1] = (byte)(buffer.Length - 2);
                    buffer[buffer.Length - 1] = GenerateChecksum(buffer); // Checksum
                    break;
                case FrameHeader.ACK:
                    this.buffer = message.ToArray();
                    break;
                case FrameHeader.CAN:
                    this.buffer = message.ToArray();
                    break;
                case FrameHeader.NAK:
                    this.buffer = message.ToArray();
                    break;
            }

            return this.buffer;
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

        /// <summary>
        /// The checksum is calculated by making an XOR of all the message bytes except for the first one. A bit-wise NOT is then executed on the result.
        /// </summary>
        /// <param name="data">The data the we is making the calculated</param>
        /// <returns>The Checksum</returns>
        private static byte GenerateChecksum(byte[] data)
        {
            int offset = 1;
            byte ret = data[offset];
            for (int i = offset + 1; i < data.Length - 1; i++)
            {
                // Xor bytes
                ret ^= data[i];
            }
            // Not result
            ret = (byte)(~ret);
            return ret;
        }


    }
}
