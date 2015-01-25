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
using ZWaveApi.Net;
using ZWaveApi.Net.Messenger;
using System.Runtime.Serialization;

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// The Command Class : ApplicationStatus
    /// </summary>
    [DataContract]
    class ApplicationStatus : GenericCommandClass
    {
        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public ApplicationStatus(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.ApplicationStatus) {
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            if ((ApplicationStatusCmd)receivedMessage.buffer[8] == ApplicationStatusCmd.Busy)
            {
                switch (receivedMessage.buffer[9])
                {
                    case 0:
                        ZWaveLog.AddEvent("Application Status : Try again later");
                        break;
                    case 1:
                        ZWaveLog.AddEvent("Application Status : Try again in " + receivedMessage.buffer[10].ToString() + " seconds");
                        break;
                    case 2:
                        ZWaveLog.AddEvent("Application Status : Request queued, will be executed later");
                        break;
                    default:
                        ZWaveLog.AddException("Application Status : Unknown status. "  + receivedMessage.buffer[9].ToString("X2"));
                        break;
                }
            }
        }
    }
}
