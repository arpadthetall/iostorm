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

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// All the Command Class that can be created.
    /// </summary>
    public enum CommandClass
    {
        Basic = 0x20,
        ControllerReplication = 0x21,
        ApplicationStatus = 0x22,
        SwitchBinary = 0x25,
        SwitchMultilevel = 0x26,
        SwitchAll = 0x27,
        SwitchToggleBinary = 0x28,
        SwitchToggleMultilevel = 0x29,
        SceneActivation = 0x2B,
        SceneActuatorConf = 0x2C,
        SensorBinary = 0x30,
        SensorMultiLevel = 0x31,
        Meter = 0x32,
        MeterPulse = 0x35,
        ThermostatMode = 0x40,
        ThermostatOperatingState = 0x42,
        ThermostatSetpoint = 0x43,
        ThermostatFanMode = 0x44,
        ThermostatFanState = 0x45,
        ClimateControlSchedule = 0x46,
        BasicWindowCovering = 0x50,
        MultiInstance = 0x60,
        Configuration = 0x70,
        Alarm = 0x71,
        ManufacturerSpecific = 0x72,
        PowerLevel = 0x73,
        Protection = 0x75,
        Lock = 0x76,
        NodeNaming = 0x77,
        Battery = 0x80,
        Clock = 0x81,
        Hail = 0x82,
        WakeUp = 0x84,
        Association = 0x85,
        Version = 0x86,
        Indicator = 0x87,
        Proprietary = 0x88,
        Language = 0x89,
        MultiInstanceAssociation = 0x8e,
        MultiCmd = 0x8F,
        EnergyProduction = 0x90,
        ManufacturerProprietary = 0x91,
        AssociationCommandConfiguration = 0x9b,
        Mark = 0xEF
    }
}
