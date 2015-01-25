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
    /// The Command Class : SwitchToggleMultilevel
    /// </summary>
    [DataContract]
    public class SwitchToggleMultilevel : GenericCommandClass, IBinarySwitch
    {

        private byte level;

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node.</param>
        public SwitchToggleMultilevel(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.SwitchToggleMultilevel) {
        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave 
        /// to the version of the library, protocol and application.
        /// </summary>
        public override void RequestState()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                                 ZWaveFunction.SendData,
                                 Node.NodeId,
                                 new byte[] { (byte)this.CommandClass, (byte)SwitchToggleMultilevelCmd.Get }),
                                 ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            if (receivedMessage.buffer[8] == (byte)SwitchMultilevelCmd.Report)
            {
                this.level = receivedMessage.buffer[9];
            }
        }

        /// <summary>
        /// Start the level changing
        /// </summary>
        /// <param name="direction">Dimmer must go up or down.</param>
        /// <param name="ignoreStartLevel">Must not start from the starting level.</param>
        /// <param name="rollover">When the limit is reached, it must start from the front or rear.</param>
        public void StartLevelChange(SwitchMultilevelDirection direction, bool ignoreStartLevel, bool rollover)
        {
            byte param = (byte)direction;
            param |= (byte)(ignoreStartLevel ? 0x20 : 0x00);
            param |= (byte)(rollover ? 0x80 : 0x00);

            ZWavePort.AddMessage(new Message(MessageType.Request,
                     ZWaveFunction.SendData,
                     Node.NodeId,
                     new byte[] { (byte)this.CommandClass, (byte)SwitchMultilevelCmd.StartLevelChange, param }),
                     ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Stop the level changing
        /// </summary>
        public void StopLevelChange()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                     ZWaveFunction.SendData,
                     Node.NodeId,
                     new byte[] { (byte)this.CommandClass, (byte)SwitchMultilevelCmd.StopLevelChange }),
                     ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Enable of disable the level change commands
        /// </summary>
        public void DoLevelChange(BinaryStatus state)
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                     ZWaveFunction.SendData,
                     Node.NodeId,
                     new byte[] { (byte)this.CommandClass, (byte)SwitchMultilevelCmd.DoLevelChange }),
                     ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Set or Get the level of the multiswitch.
        /// </summary>
        [DataMember]
        public byte Level
        {
            get { return this.level; }

            set
            {
                this.level = value;

                if (this.level >= 0x63)
                    this.level = 0x63;

                ZWavePort.AddMessage(new Message(MessageType.Request,
                                                 ZWaveFunction.SendData,
                                                 Node.NodeId,
                                                 new byte[] { (byte)this.CommandClass, (byte)SwitchMultilevelCmd.Set, (byte)this.level }),
                                                 ZWaveMessagePriority.Interactive);
            }
           }

        #region IBinarySwitch Members

        /// <summary>
        /// Get the status of the Binary Switch On, Off or Pct. of level
        /// </summary>
        [DataMember]
        public BinaryStatus State
        {
            get
            {
                if (this.level >= 0x63)
                    return BinaryStatus.On;
                else
                    return BinaryStatus.Off;
            }

            set
            {
                if (value == BinaryStatus.On)
                    ON();
                else
                    OFF();
            }
        }

        /// <summary>
        /// Set the Command class to ON.
        /// </summary>
        public void ON()
        {
            this.Level = 0x63;   // This is the highest value you can give to the multiswitch.
		}

        /// <summary>
        /// Set the Command class to OFF.
        /// </summary>
        public void OFF()
        {
            this.Level = 0x63;   // This is the highest value you can give to the multiswitch.
		}

        public void Toggle()
        {
            if (level == 0x00)  // The Swicht is off state
                ON();
            else
                OFF();
        }

        #endregion

    }
}
