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
using System.Collections.Generic;
using System.Text;
using ZWaveApi.Net.CommandClasses;

namespace ZWaveApi.Net.CommandClasses
{
    /// <summary>
    /// The interface on the Binary Switch.
    /// </summary>
    public interface IBinarySwitch
    {
        /// <summary>
        /// Get the status of the Binary Switch
        /// </summary>
        BinaryStatus State { get; }

        /// <summary>
        /// Set the binary Swicth to ON.
        /// </summary>
        void ON();

        /// <summary>
        /// Set the binary Swicth to OFF.
        /// </summary>
        void OFF();

        /// <summary>
        /// Switch between On and Off.
        /// </summary>
        void Toggle();

    }
}