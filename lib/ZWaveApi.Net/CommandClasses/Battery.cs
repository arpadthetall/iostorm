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

using System;
using System.Runtime.Serialization;
using ZWaveApi.Net;
using ZWaveApi.Net.Messenger;

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// The Command Class : Battery
    /// </summary>
    [DataContract]
    public class Battery : GenericCommandClass
    {
        /// <summary>
        /// The level of the bettery.
        /// </summary>
        [DataMember]
        public byte level { get; private set; }

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public Battery(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.Battery) {
        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave.
        /// </summary>
        public override void RequestState()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                                 ZWaveFunction.SendData,
                                 Node.NodeId,
                                 new byte[] { (byte)this.CommandClass, (byte)BatteryCmd.Get }),
                                 ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            if ((BatteryCmd)receivedMessage.buffer[8] == BatteryCmd.Report)
            {
                this.level = receivedMessage.buffer[9];

                // Devices send 0xff instead of zero for a low battery warning.
                if (this.level == 0xff)
                {
                    this.level = 0x00;
                    ZWaveLog.AddWarning("Battery: Low battery. " + receivedMessage.buffer[8].ToString("X2"));
                }
            }
        }
    }
}
