using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Config;
using Qlue.Logging;
using Newtonsoft.Json;

namespace IoStorm.Nodes
{
    public class IrOutputNode : INode
    {
        private ILog log;
        private NodeConfig config;
        private Dictionary<string, List<IrOutputMapping>> mapping;
        private string outputPort;
        private IHub hub;
        private bool toggle;

        public IrOutputNode(ILogFactory logFactory, NodeConfig config, IHub hub)
        {
            this.log = logFactory.GetLogger("IrOutputNode");
            this.config = config;
            string mappingFile = config.Settings["IrMapping"];
            this.outputPort = config.Settings["OutputPort"];
            this.hub = hub;

            string content = File.ReadAllText("Config\\" + mappingFile);

            var loadedMapping = JsonConvert.DeserializeObject<List<IrOutputMapping>>(content);

            this.mapping = new Dictionary<string, List<IrOutputMapping>>();
            foreach (var cfgItem in loadedMapping)
            {
                string key = cfgItem.Payload;

                if (!cfgItem.Payload.StartsWith("IoStorm.Payload."))
                    key = "IoStorm.Payload." + cfgItem.Payload;

                List<IrOutputMapping> outputs;
                lock (this.mapping)
                {
                    if (!this.mapping.TryGetValue(key, out outputs))
                    {
                        outputs = new List<IrOutputMapping>();

                        this.mapping.Add(key, outputs);
                    }
                }
                outputs.Add(cfgItem);
            }
        }

        public void Incoming(Payload.IPayload payload)
        {
            string typeName = payload.GetType().FullName;

            List<IrOutputMapping> outputs;
            lock (this.mapping)
            {
                if (!this.mapping.TryGetValue(typeName, out outputs))
                    return;
            }

            foreach (var output in outputs)
            {
                if (output.Match != null && output.Match.Any())
                {
                    // Check match
                    foreach (var match in output.Match)
                    {
                        var property = payload.GetType().GetProperty(match.Key);
                        if (property == null)
                            continue;

                        object value = property.GetValue(payload);

                        if (value.ToString() != match.Value)
                            continue;

                        // Match
                        int repeat;
                        Payload.IIRProtocol irCommand = GetIrProtocol(output.Transmit, out repeat);

                        if (irCommand != null)
                        {
                            this.hub.SendPayload(this.config.InstanceId, this.config.PluginInstanceId,
                                new Payload.IRCommand
                                {
                                    PortId = this.outputPort,
                                    Repeat = repeat,
                                    Command = irCommand
                                }
                            );
                        }
                    }
                }
            }
        }

        private Payload.IIRProtocol GetIrProtocol(IrOutputTransmit input, out int repeat)
        {
            repeat = 1;
            switch (input.Protocol)
            {
                case "Sony12":
                    repeat = 2;
                    return new IoStorm.IRProtocol.Sony12(input.Address, input.Command);

                case "Sony15":
                    repeat = 2;
                    return new IoStorm.IRProtocol.Sony15(input.Address, input.Command);

                case "Sony20":
                    repeat = 2;
                    return new IoStorm.IRProtocol.Sony20(input.Address, input.Command, input.Extended);

                case "NECx":
                    repeat = 2;
                    return new IoStorm.IRProtocol.NECx(input.AddressH, input.AddressL, input.Command);

                case "Nokia32":
                    toggle = !toggle;
                    return new IoStorm.IRProtocol.Nokia32(input.AddressH, input.AddressL, input.Command, input.Extended, toggle);

                default:
                    this.log.Warn("Unknown IR protocol {0}", input.Protocol);
                    return null;
            }
        }

        public string InstanceId
        {
            get { return this.config.InstanceId; }
        }
    }
}
