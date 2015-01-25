using System;
using System.Collections.Generic;
using System.Text;

namespace ZWaveApi.Net.CommandClasses.Events
{
    public class MultiLevelChangedEventArgs : GenericEventArgs
    {
        public int Level { get; protected set; } 

        public MultiLevelChangedEventArgs(int level) {
            this.Level = level;
        }
    }
}
