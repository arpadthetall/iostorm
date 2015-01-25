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
                    case 0x01:  // Temperature is received.
                        this.Temperatur = receivedMessage.buffer[10];
                        break;

                    case 0x02:  // Sensor is received.
                        this.Sensor = receivedMessage.buffer[10];
                        break;

                    case 0x03:  // Luminace is received.
                        this.Luminace = receivedMessage.buffer[10];
                        break;

                    case 0x04:  // Power is received.
                        this.Power = receivedMessage.buffer[10];
                        break;

                    case 0x05:  // Humidity is received.
                        this.Humidity = receivedMessage.buffer[10];
                        break;

                    case 0x11:  // Carbon Monxide is received.
                        this.CarbonMonxide = receivedMessage.buffer[10];
                        break;

 */

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    ///  .
    /// </summary>
    public enum SensorType {
        Unknown                 = 0x00,
        Temperature             = 0x01,
        General                 = 0x02,
        Luminance               = 0x03,
        Power                   = 0x04,
        RelativeHumidity        = 0x05,
        Velocity                = 0x06,
        Direction               = 0x07,
        AtmosphericPressure     = 0x08,
        BarometricPressure      = 0x09,
        SolarRadiation          = 0x0A,
        DewPoint                = 0x0B,
        RainRate                = 0x0C,
        TideLevel               = 0x0D,
        Weight                  = 0x0E,
        Voltage                 = 0x0F,
        Current                 = 0x10,
        CO2                     = 0x11,
        AirFlow                 = 0x12,
        TankCapacity            = 0x13,
        Distance                = 0x14
    }
}
