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
    /// The Command Class : ManufacturerSpecific
    /// </summary>
    [DataContract]
    public class ManufacturerSpecific : GenericCommandClass
    {
        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public ManufacturerSpecific(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.ManufacturerSpecific) {
        }


        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            Console.WriteLine("Command Recieved: ManufacturerSpecific");
        }

    }
}