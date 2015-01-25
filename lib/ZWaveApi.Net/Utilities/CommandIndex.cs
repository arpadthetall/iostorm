using System;
using System.Collections.Generic;
using System.Text;
using ZWaveApi.Net.CommandClasses;

namespace ZWaveApi.Net.Utilities {
    public struct CommandIndex {
        public readonly byte NodeId;
        public readonly CommandClass Command;
        public readonly byte Instance;
        public CommandIndex(byte nodeId, CommandClass command, byte instance) { NodeId = nodeId; Command = command; Instance = instance; }

        public string StringId() {
            return "zn:" + this.NodeId + ":" + (byte)this.Command + ":" + this.Instance;
        }

        public override string ToString() {
            return "Z-Wave Node " + this.NodeId + ": " + this.Command + " " + this.Instance;
        }
    }
}
