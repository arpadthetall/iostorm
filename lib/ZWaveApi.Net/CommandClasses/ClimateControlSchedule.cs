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
    /// The Command Class : ClimateControlSchedule
    /// </summary>
    [DataContract]
    public class ClimateControlSchedule : GenericCommandClass
    {
        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public ClimateControlSchedule(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.ClimateControlSchedule) {
        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave.
        /// </summary>
        public override void RequestState()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                     ZWaveFunction.SendData,
                     this.Node.NodeId,
                     new byte[] { (byte)this.CommandClass, (byte)ClimateControlScheduleCmd.Get }),
                     ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            switch ((ClimateControlScheduleCmd)receivedMessage.buffer[8])
            {
                case ClimateControlScheduleCmd.Report:
                    break;
                case ClimateControlScheduleCmd.ChangedReport:
                    break;
                case ClimateControlScheduleCmd.OverrideReport:
                    break;
                default:
                    ZWaveLog.AddException("Climate Control Schedule: Unkown command. " + receivedMessage.buffer[8].ToString("X2"));
                    break;
            }
        }
    }
}