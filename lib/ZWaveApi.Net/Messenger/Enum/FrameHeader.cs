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

namespace ZWaveApi.Net.Messenger
{
    /// <summary>
    /// The Communition types that´s use by ZWave.
    /// </summary>
    public enum FrameHeader
    {
        /// <summary>
        /// Start of frame
        /// </summary>
        SOF = 0x01,
        /// <summary>
        /// Acknowledge
        /// </summary>
        ACK = 0x06,
        /// <summary>
        /// Negative acknowledge
        /// </summary>
        NAK = 0x15,
        /// <summary>
        /// Cancel
        /// </summary>
        CAN = 0x18
    }
}