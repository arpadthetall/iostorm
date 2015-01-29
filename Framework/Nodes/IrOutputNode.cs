using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Config;
using Newtonsoft.Json;

namespace IoStorm.Nodes
{
    public class IrOutputNode : INode
    {
        public IrOutputNode(NodeConfig config)
        {
            string mappingFile = config.Settings["IrMapping"];

            string content = File.ReadAllText("Config\\" + mappingFile);

            var obj = JsonConvert.DeserializeObject<Dictionary<string, IrOutputMapping>>(content);
        }
    }
}
