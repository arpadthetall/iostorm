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

namespace ZWaveApi.Net
{
    /// <summary>
    /// The device type known as generic type in the ZWave.
    /// </summary>
    public enum GenericType
    {
        Unknown = 0x00,
        PortableRemote = 0x01,
        StaticController = 0x02,
        AVControlPoint = 0x03,
        RoutingSlave = 0x04,
        Display = 0x06,
        GarageDoor = 0x07,
        Thermostat = 0x08,
        WindowCovering = 0x09,
        RepeaterSlave = 0x0F,
        SwitchBinary = 0x10,
        SwitchMultiLevel = 0x11,
        SwitchRemote = 0x12,
        SwitchToggle = 0x13,
        SensorBinary = 0x20,
        SensorMultiLevel = 0x21,
        WaterControl = 0x22,
        MeterPulse = 0x30,
        EntryControl = 0x40,
        SemiInteroperable = 0x50,
        SmokeDetector = 0xA1,
        NonInteroperable = 0xFF
    }
}
