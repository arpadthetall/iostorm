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
    /// The Command Class : MultiInstance
    /// </summary>
    [DataContract]
    public class MultiInstance : GenericCommandClass
    {

        private Dictionary<CommandClass, int> _commands;

        private int version = 0;

        //In the node initialization, we are doing this in order, 
        // so to speed up the process keep the last requested multiinstance in a general variable
        private CommandClass _lastCommand;
        private int _lastInstanceCount;

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public MultiInstance(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.MultiInstance) {

            this._commands = new Dictionary<CommandClass,int>();
            // TODO Implement multi channel (version 2)
            this.version = 1; //node.GetCommandVersion(this.CommandClass);

        }

        public void RequestCommandClassInstances(CommandClass Command) {
            switch (this.version) {
                case 0:
                case 1: //Multi Instance
                    ZWavePort.AddMessage(new Message(MessageType.Request,
                                    ZWaveFunction.SendData,
                                    this.Node.NodeId,
                                    new byte[] { (byte)this.CommandClass, (byte)MultiInstanceCmd.Get, (byte)Command }, TransmitOptions.Ack | TransmitOptions.AutoRoute, ZWavePort.GetCallbackId()),
                                    ZWaveMessagePriority.Interactive);

                    break;

                case 2: //Multi Channel
                    break;

                default:
                    break;
            }

        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave.
        /// </summary>
        public override void RequestState()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                                             ZWaveFunction.SendData,
                                             this.Node.NodeId,
                                             new byte[] { (byte)this.CommandClass, (byte)MultiInstanceCmd.Get }),
                                             ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            try {
                switch ((MultiInstanceCmd)(receivedMessage.buffer[8])) {
                    case MultiInstanceCmd.Report:
                        this._lastCommand = (CommandClass)receivedMessage.buffer[9];
                        this._lastInstanceCount = receivedMessage.buffer[10];
                        if (this._commands.ContainsKey(_lastCommand)) {
                            this._commands[_lastCommand] = _lastInstanceCount;
                        } else {
                            this._commands.Add(_lastCommand, _lastInstanceCount);
                        }

                        break;
                    case MultiInstanceCmd.CmdEncap:
                        CommandClass command = (CommandClass)receivedMessage.buffer[10];
                        byte instanceId = receivedMessage.buffer[9];

                        byte[] buffer = new byte[receivedMessage.buffer.Length - 3];
                        Array.Copy(receivedMessage.buffer, buffer, 6);
                        buffer[6] = (byte)(receivedMessage.buffer.Length - 10);
                        Array.Copy(receivedMessage.buffer, 10, buffer, 7, buffer[6]);
                        Message message = new Message(buffer, instanceId);
                        this.Node.HandleMessage(message);
                        break;
                }
            } catch (Exception e) {
                ZWaveLog.AddException(e);
            }
        }

        public Message EncapsulateMessage(GenericCommandClass commandClass, Message message) {

            byte[] buffer;
            switch (this.version) {
                case 0: //The default if no version information is available
                case 1: //MULTI_INSTANCE
                    buffer = new byte[message.command.Length + 3];

                    buffer[0] = (byte)this.CommandClass;
                    buffer[1] = (byte)MultiInstanceCmd.CmdEncap;
                    buffer[2] = (byte)commandClass.InstanceId;

                    int i = 0;
                    for (i = 0; i < message.command.Length; i++) {
                        buffer[3 + i] = message.command[i];
                    }

                    return new Message(MessageType.Request,
                                        ZWaveFunction.SendData,
                                        this.Node.NodeId,
                                        buffer);
                    
                case 2: //MULTI_CHANNEL


                    break;
                default:
                    throw new NotImplementedException("MultiInstance Version [" + this.version + "] is not implemented");
            }

            return message;
        }

        public int CommandInstances(CommandClass command) {
            if (this._lastCommand == command) {
                return this._lastInstanceCount;
            } else if (this._commands.ContainsKey(command)) {
                return this._commands[command];
            } else {
                return 0;
            }

        }
    }
}