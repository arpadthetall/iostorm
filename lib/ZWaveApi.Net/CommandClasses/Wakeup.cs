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
using System.Threading;

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// The Command Class : Wakeup
    /// </summary>
    [DataContract]
    public class WakeUp : GenericCommandClass
    {
        /// <summary>
        /// Received Wakeup Interval report from node.
        /// </summary>
        [DataMember]
        public uint interval  { get; private set; }



        private List<Message> _messages;

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node.</param>
        public WakeUp(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.WakeUp) {
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
                                 new byte[] { (byte)this.CommandClass, (byte)SwitchAllCmd.Set, (byte)WakeupCmd.IntervalGet }),
                                 ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            switch ((WakeupCmd)receivedMessage.buffer[8])
            {
                case WakeupCmd.IntervalReport:
                    this.interval = ((uint)receivedMessage.buffer[9]) << 16;
                    this.interval |= (((uint)receivedMessage.buffer[10]) << 8);
                    this.interval |= (uint)receivedMessage.buffer[11];
                    break;

                case WakeupCmd.Notification:
                    ZWaveLog.AddEvent("Wakeup: Notification");                    

                    // We are in a response process so we need to put the initialization in another thread:
                    ThreadPool.QueueUserWorkItem(new WaitCallback(this.InitializeThreadStart));

                    break;
                
                default:
                    ZWaveLog.AddException("Unknown Message on Command Class Wakeup : " + receivedMessage.buffer[8].ToString("X2"));
                    break;
            }
        }

        // If the node is not initialized we need to wait a little bit in order to allow to be up and running before requesting states
        private void InitializeThreadStart(Object stateInfo) {

            // Run node initialization (if node is already initialized this will do nothing)
            this.Node.Initialize();


            // If the node is not initialized we need to wait a little bit in order to allow to be up and running before requesting states
            System.Threading.Thread.Sleep(1400);

            // Get state for all command classes on wake up (need time between)
            foreach (CommandClasses.GenericCommandClass command in Node.CommandClasses.Values) {
                Console.WriteLine("Request state (WU): " + command.CommandClass);
                command.RequestState();
                System.Threading.Thread.Sleep(500);
            }
        }

    }
}
