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
    /// The function command that MultiInstanceAssociationCmd can use.
    /// </summary>
    public enum MultiInstanceCmd
    {
        Get = 0x04,
        Report = 0x05,
        CmdEncap = 0x06,
        MULTI_CHANNEL_END_POINT_GET = 0x07,
        MULTI_CHANNEL_END_POINT_REPORT = 0x08,
        MULTI_CHANNEL_CMD_ENCAP = 0x0D,
        MULTI_CHANNEL_CAPABILITY_REPORT = 0x0A
    }
}

