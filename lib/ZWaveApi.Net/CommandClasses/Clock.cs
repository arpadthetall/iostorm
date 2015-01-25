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
    /// The Command Class : Clock
    /// </summary>
    [DataContract]
    public class Clock : GenericCommandClass
    {
        private int day;
        private int hour;
        private int minute;

        /// <summary>
        /// the Constructor.
        /// </summary>
        /// <param name="nodeId">The Id of the node</param>
        public Clock(ZWaveNode node, byte instanceId)
            : base(node, instanceId, CommandClass.Clock) {
        }

        /// <summary>
        /// Ask the Request State of the Class from the ZWave.
        /// </summary>
        public override void RequestState()
        {
            ZWavePort.AddMessage(new Message(MessageType.Request,
                     ZWaveFunction.SendData,
                     Node.NodeId,
                     new byte[] { (byte)this.CommandClass, (byte)ClockCmd.Get }),
                     ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Decode the message to this command clase.
        /// </summary>
        /// <param name="message">The message to the command Class</param>
        public override void HandleMessage(Message receivedMessage)
        {
            if (receivedMessage.buffer[8] == (byte)ClockCmd.Report)
            {
                Day = (receivedMessage.buffer[9] >> 5);
                Hour = receivedMessage.buffer[9] & 0x1f;
                Minute = receivedMessage.buffer[10];
            }
        }

        /// <summary>
        /// Send the Clock value to the ZWave
        /// </summary>
        /// <param name="Day">Which day is it. Value from 1(Monday) to 7(Sunday)</param>
        /// <param name="Hour">The Hour for the day. Value from 0 and 23</param>
        /// <param name="Minuter">The Minute of the Hour. Value from 0 and 60</param>
        public void SetClock(int day, int hour, int minute)
        {
            this.Day = day;
            this.Hour = hour;
            this.Minute = minute;

            byte dayHour = (byte)((this.Day << 5) | this.Hour);

            ZWavePort.AddMessage(new Message(MessageType.Request,
                     ZWaveFunction.SendData,
                     Node.NodeId,
                     new byte[] { (byte)this.CommandClass, (byte)ClockCmd.Set, dayHour, (byte)this.Minute }),
                     ZWaveMessagePriority.Interactive);
        }

        /// <summary>
        /// Send the Clock value to the ZWave (Sysdate)
        /// </summary>
        public void SetClock()
        {
            DateTime dt = DateTime.Now;
           
            SetClock((int)dt.DayOfWeek, dt.Hour, dt.Minute);
        }

        [DataMember]
        public int Day
        {
            get { return day; }
            set
            {
                if (value < 1 || value > 7)
                    ZWaveLog.AddException("The Value of Day has be between 1(Monday) And 7(Sunday)");
                day = value;
            }
        }

        [DataMember]
        public int Hour
        {
            get { return hour; }
            set
            {
                if (value < 0 || value > 23)
                    throw new ArgumentException("The Value of Hour has be between 0 And 23");
                hour = value;
            }
        }

        [DataMember]
        public int Minute
        {
            get { return minute; }
            set
            {
                if (value < 0 || value > 59)
                    throw new ArgumentException("The Value of Minute has be between 0 And 59");
                minute = value;
            }
        }
    }
}
