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

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// The Value of the SwitchAll command class.
    /// </summary>
    public enum SwitchAllCmd
    {
        Set = 0x01,
        Get = 0x02,
        Report = 0x03,
        On = 0x04,
        Off = 0x05
    }
}
