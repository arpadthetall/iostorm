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
    /// The Command Class : MultilevelSensor
    /// </summary>
    [DataContract]
    public class SensorMultiLevel : GenericCommandClass
    {
        [DataMember]
        public SensorType Type { get; private set; }
        
        [DataMember]
        public int Level { get; private set; }
        
        [DataMember]
        public string Units { get; private set; }


        public EventHandler<Events.MultiLevelChangedEventArgs> Changed;

        private string[] _tankCapacityUnits = { "l", "cbm", "gal" };
        private string[] _distanceUnits = { "m", "cm", "ft" };

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public SensorMultiLevel(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.SensorMultiLevel) {


        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave.
        /// </summary>
        public override void RequestState()
        {
            this.Node.SendMessage(this, new Message(MessageType.Request,
                                 ZWaveFunction.SendData,
                                 Node.NodeId,
                                 new byte[] { (byte)this.CommandClass, (byte)SensorMultilevelCmd.Get }),
                                 ZWaveMessagePriority.Interactive);
        }

        // Invoke the Changed event;
        protected virtual void OnChanged(Events.MultiLevelChangedEventArgs e) {
            if (Changed != null)
                Changed(this, e);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {

            if (receivedMessage.buffer[8] == (byte)SensorMultilevelCmd.Report)
            {

                byte scale = 0;
                byte[] values = Utilities.Extensions.ArraySlice(receivedMessage.buffer, 10);

                this.Level = Utilities.ByteFunctions.ExtractValue(values, ref scale);
                this.Type = (SensorType)receivedMessage.buffer[9];

                switch (this.Type) {
                    case SensorType.Temperature:            this.Units = (scale == 1 ? "F" : "C"); break;
                    case SensorType.General:                this.Units = (scale == 1 ? "" : "%"); break;
                    case SensorType.Luminance:              this.Units = (scale == 1 ? "lux" : "%"); break;
                    case SensorType.Power:                  this.Units = (scale == 1 ? "BTU/h" : "W"); break;
                    case SensorType.RelativeHumidity:       this.Units = ("%"); break;
                    case SensorType.Velocity:               this.Units = (scale == 1 ? "mph" : "m/s"); break;
                    case SensorType.Direction:              this.Units = (""); break;
                    case SensorType.AtmosphericPressure:    this.Units = (scale == 1 ? "inHg" : "kPa"); break;
                    case SensorType.BarometricPressure:     this.Units = (scale == 1 ? "inHg" : "kPa"); break;
                    case SensorType.SolarRadiation:         this.Units = ("W/m2"); break;
                    case SensorType.DewPoint:               this.Units = (scale == 1 ? "in/h" : "mm/h"); break;
                    case SensorType.RainRate:               this.Units = (scale == 1 ? "F" : "C"); break;
                    case SensorType.TideLevel:              this.Units = (scale == 1 ? "ft" : "m"); break;
                    case SensorType.Weight:                 this.Units = (scale == 1 ? "lb" : "kg"); break;
                    case SensorType.Voltage:                this.Units = (scale == 1 ? "mV" : "V"); break;
                    case SensorType.Current:                this.Units = (scale == 1 ? "mA" : "A"); break;
                    case SensorType.CO2:                    this.Units = ("ppm"); break;
                    case SensorType.AirFlow:                this.Units = (scale == 1 ? "cfm" : "m3/h"); break;
                    case SensorType.TankCapacity:           this.Units = (this._tankCapacityUnits[scale]); break;
                    case SensorType.Distance:               this.Units = (this._distanceUnits[scale]); break;
                    default: break;
                }
                this.OnChanged(new Events.MultiLevelChangedEventArgs(this.Level));
                ZWaveApi.Net.ZWaveLog.AddEvent("Multilevel Sensor Report : " + this.Type + " = " + this.Level + this.Units);


            }

        }

    }
}
