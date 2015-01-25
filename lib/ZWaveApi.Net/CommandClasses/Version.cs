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
using System.Collections.Generic;

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// The Command Class : Version
    /// </summary>
    [DataContract]
    public class Version : GenericCommandClass
    {
        /// <summary>
        /// Return the version of the library on this node.
        /// </summary>
        [DataMember]
        public string Library { get; private set; }

        /// <summary>
        /// Return the version of the application on this node.
        /// </summary>
        [DataMember]
        public string Application { get; private set; }

        /// <summary>
        /// Return the Version of the protocol on this node.
        /// </summary>
        [DataMember]
        public string Protocol { get; private set; }

        /// <summary>
        /// Return the version of this node.
        /// </summary>
        [DataMember]
        private byte version;


        private Dictionary<CommandClass, int> _commands;

        /// <summary>
        /// the Constructor
        /// </summary>
        /// <param name="nodeId">The Id of the node.</param>
        public Version(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.Version) {

            this._commands = new Dictionary<CommandClass,int>();
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
                                             new byte[] { (byte)this.CommandClass, (byte)SwitchAllCmd.Set, (byte)VersionCmd.Get }),
                                             ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave 
        /// to the version of the command class.
        /// </summary>
        public void RequestCommandClassVersion(CommandClass Command)
        {


            ZWavePort.AddMessage(new Message(MessageType.Request,
                                             ZWaveFunction.SendData,
                                             Node.NodeId,
                                             new byte[] { (byte)this.CommandClass, (byte)VersionCmd.CommandClassGet, (byte)Command }, TransmitOptions.Ack | TransmitOptions.AutoRoute, ZWavePort.GetCallbackId()),
                                             ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            
            switch ((VersionCmd)receivedMessage.buffer[8])
            {
                case VersionCmd.Report:
                    this.Library = receivedMessage.buffer[9].ToString("d");
                    this.Protocol = receivedMessage.buffer[10].ToString("d") + "." + receivedMessage.buffer[11].ToString("d");
                    this.Application = receivedMessage.buffer[12].ToString("d") + "." + receivedMessage.buffer[13].ToString("d");
                    break;

                case VersionCmd.CommandClassReport:
                    CommandClass command = (CommandClass)receivedMessage.buffer[9];
                    int version = receivedMessage.buffer[10];
                    //this.version = receivedMessage.buffer[10]; this is the commandclass id

                    if (this._commands.ContainsKey(command)) {
                        this._commands[command] = version;
                    } else {
                        this._commands.Add(command,version);
                    }
                    this.version = receivedMessage.buffer[10];
                    break;

                default:
                    ZWaveLog.AddException("Unknown VersionCmd : " + receivedMessage.buffer[8].ToString("X2"));
                    break;
            }            
        }

        /// <summary>
        /// Return the Version of the Command Class on this node.
        /// </summary>
        public string CommandClassVersion
        {
            get { return this.version.ToString("d"); }
        }

        public int CommandVersion(CommandClass command) {
            if (this._commands.ContainsKey(command)) {
                return this._commands[command];
            } else {
                return 0; 
            }

        }
    }
}
