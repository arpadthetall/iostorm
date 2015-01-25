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
    /// The Command Class : Lock
    /// </summary>
    [DataContract]
    public class Lock : GenericCommandClass, IBinarySwitch
    {

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public Lock(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.Lock) {
        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave.
        /// </summary>
        public override void RequestState()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                                 ZWaveFunction.SendData,
                                 this.Node.NodeId,
                                 new byte[] { (byte)this.CommandClass, (byte)LockCmd.Get }),
                                 ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            if (receivedMessage.buffer[8] == (byte)SwitchBinaryCmd.Report)
            {
                this.State = (BinaryStatus)receivedMessage.buffer[9];
            }

        }

        #region IBinarySwitch Members
        
        /// <summary>
        /// Get the status of the Binary all
        /// </summary>
        [DataMember]
        public BinaryStatus State{ get; private set;}

        /// <summary>
        /// Set the binary all to ON.
        /// </summary>
        public void ON()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                                 ZWaveFunction.SendData,
                                 Node.NodeId,
                                 new byte[] { (byte)this.CommandClass, (byte)LockCmd.Set, (byte)BinaryStatus.On }),
                                 ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Set the binary all to OFF.
        /// </summary>
        public void OFF()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                                 ZWaveFunction.SendData,
                                 Node.NodeId,
                                 new byte[] { (byte)this.CommandClass, (byte)LockCmd.Set, (byte)BinaryStatus.Off }),
                                 ZWaveMessagePriority.Interactive);
        }

        public void Toggle()
        {
            if (this.State == BinaryStatus.Off)
                ON();
            else
                OFF();
        }

        #endregion
    }
}
