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
using System.Runtime.InteropServices;

namespace ZWaveApi.Net.Messenger
{
    /// <summary>
    /// This struct hold the rank of byte that is send to the ZWave Controller
    /// with a priority
    /// </summary>
    /// <remarks>
    /// Based on PriorityQueue.cs by Jim Mischel
    /// </remarks>
    /// <typeparam name="TValue">The rank of byte</typeparam>
    /// <typeparam name="TPriority">The Priority of queue item.</typeparam>
    [Serializable]
    [ComVisible(false)]
    public struct PriorityQueueItem<TValue, TPriority>
    {
        private TValue _value;

        public TValue Value
        {
            get { return _value; }
        }

        private TPriority _priority;

        public TPriority Priority
        {
            get { return _priority; }
        }

        internal PriorityQueueItem(TValue val, TPriority pri)
        {
            this._value = val;
            this._priority = pri;
        }
    }
}
