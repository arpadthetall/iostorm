using System;
using System.Collections.Generic;
using System.Text;

namespace ZWaveApi.Net {
    class CommandClassSetting {
        public string Type     { get; set; }
        public byte Index { get; set; }
        public string Genre { get; set; }
        public string Label { get; set; }
        public string Units { get; set; }
        public byte Min { get; set; }
        public byte Max { get; set; }
        public string Help { get; set; }
        public Dictionary<byte, string> Items { get; set; }

        public byte Value;

        CommandClassSetting() {
            Items = new Dictionary<byte, string>();
            //Value = byte[1];
        }


    }
}
