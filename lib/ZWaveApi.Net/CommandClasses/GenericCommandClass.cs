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
using ZWaveApi.Net.Messenger;

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// This class is the abstract to all the Commands, just be ZWave.
    /// </summary>
    [DataContract]
    [KnownType(typeof(Alarm))]
    [KnownType(typeof(ApplicationStatus))]
    [KnownType(typeof(Association))]
    [KnownType(typeof(AssociationCommandConfiguration))]
    [KnownType(typeof(Basic))]
    [KnownType(typeof(BasicWindowCovering))]
    [KnownType(typeof(Battery))]
    [KnownType(typeof(ClimateControlSchedule))]
    [KnownType(typeof(Clock))]
    [KnownType(typeof(Configuration))]
    [KnownType(typeof(ControllerReplication))]
    [KnownType(typeof(EnergyProduction))]
    [KnownType(typeof(Hail))]
    [KnownType(typeof(Indicator))]
    [KnownType(typeof(Language))]
    [KnownType(typeof(Lock))]
    [KnownType(typeof(ManufacturerSpecific))]
    [KnownType(typeof(Meter))]
    [KnownType(typeof(MeterPulse))]
    [KnownType(typeof(MultiCmd))]
    [KnownType(typeof(MultiInstance))]
    [KnownType(typeof(MultiInstanceAssociation))]
    [KnownType(typeof(NodeNaming))]
    [KnownType(typeof(PowerLevel))]
    [KnownType(typeof(Proprietary))]
    [KnownType(typeof(Protection))]
    [KnownType(typeof(SensorBinary))]
    [KnownType(typeof(SensorMultiLevel))]
    [KnownType(typeof(SwitchAll))]
    [KnownType(typeof(SwitchBinary))]
    [KnownType(typeof(SwitchMultilevel))]
    [KnownType(typeof(SwitchToggleBinary))]
    [KnownType(typeof(SwitchToggleMultilevel))]
    [KnownType(typeof(ThermostatFanMode))]
    [KnownType(typeof(ThermostatFanState))]
    [KnownType(typeof(ThermostatMode))]
    [KnownType(typeof(ThermostatOperatingState))]
    [KnownType(typeof(ThermostatSetpoint))]
    [KnownType(typeof(Version))]
    [KnownType(typeof(WakeUp))]
    public abstract class GenericCommandClass
    {
        
        public byte InstanceId { get; protected set; }

        public ZWaveNode Node { get; protected set; }

        [DataMember]
        public byte NodeId { get { return Node.NodeId; } set { } }

        [DataMember]
        public CommandClass CommandClass { get; protected set; }

        /// <summary>
        /// Create the Generic Command class
        /// </summary>
        protected GenericCommandClass() {
        }

        /// <summary>
        /// Create the Generic Command class
        /// </summary>
        public GenericCommandClass(ZWaveNode node, byte instanceId, CommandClass commandClass) {
            this.Node = node;
            this.InstanceId = instanceId;
            this.CommandClass = commandClass;
        }

        /// <summary>
        /// The Virtual void og the RequestState.
        /// </summary>
        public virtual void RequestState()
        {
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public virtual void HandleMessage(Message receivedMessage)
        {
            Console.WriteLine("UNHANDLED MESSAGE");
        }

        public virtual string ToXML() {
            return "";
        }

        public virtual string ParseXML() {
            return "";
        }
    }
}