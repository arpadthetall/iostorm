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
        private int powerOnDelayMs;
        private Queue<IrOutputTransmit> outputQueue;
        private bool powerIsOn;

        public IrOutputNode(ILogFactory logFactory, NodeConfig config, IHub hub)
        {
            this.log = logFactory.GetLogger("IrOutputNode");
            this.config = config;
            string irConfigFile = config.Settings["IrConfig"];
            this.outputPort = config.Settings["OutputPort"];
            this.hub = hub;

            string content = File.ReadAllText(Path.Combine(hub.ConfigPath, irConfigFile));

            var irConfig = JsonConvert.DeserializeObject<IrConfig>(content);

            this.mapping = new Dictionary<string, List<IrOutputMapping>>();
            this.powerOnDelayMs = irConfig.PowerOnDelayMs;

            foreach (var cfgItem in irConfig.IrMapping)
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
                bool isMatch = true;
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
                        {
                            isMatch = false;
                            break;
                        }
                    }
                }

                if (isMatch)
                {
                    // Match

                    Queue<IrOutputTransmit> queue;
                    lock (this)
                    {
                        queue = this.outputQueue;
                    }

                    if (queue != null)
                    {
                        // Queue for after power on delay
                        queue.Enqueue(output.Transmit);
                    }
                    else
                    {
                        // Send now
                        SendOutput(output.Transmit);
                    }

                    bool? newPower = null;
                    if (payload is Payload.Power.Set)
                    {
                        var powerPayload = (Payload.Power.Set)payload;

                        newPower = powerPayload.Value;
                    }
                    if (payload is Payload.Power.Toggle)
                    {
                        newPower = !this.powerIsOn;
                    }

                    if (newPower.HasValue)
                    {
                        if (!this.powerIsOn && newPower.Value)
                        {
                            // Power is turned on, see if we should delay commands
                            if (this.powerOnDelayMs > 0)
                            {
                                lock (this)
                                {
                                    if (this.outputQueue == null)
                                        this.outputQueue = new Queue<IrOutputTransmit>();
                                }

                                Task.Delay(this.powerOnDelayMs).ContinueWith(x =>
                                        {
                                            lock (this)
                                            {
                                                queue = this.outputQueue;
                                                this.outputQueue = null;
                                            }

                                            if (queue != null)
                                                SendQueue(queue);
                                        });
                            }
                        }

                        this.powerIsOn = newPower.Value;
                    }
                }
            }
        }

        private void SendQueue(Queue<IrOutputTransmit> queue)
        {
            try
            {
                while (queue.Count > 0)
                {
                    SendOutput(queue.Dequeue());
                }
            }
            catch (Exception ex)
            {
                this.log.WarnException("Failed to send Ir output", ex);
            }
        }

        private void SendOutput(IrOutputTransmit command)
        {
            int repeat;
            Payload.IIRProtocol irCommand = GetIrProtocol(command, out repeat);

            if (irCommand != null)
            {
                this.hub.SendPayload(
                    originatingInstanceId: this.config.InstanceId,
                    destinationInstanceId: this.config.PluginInstanceId,
                    payload: new Payload.IRCommand
                    {
                        PortId = this.outputPort,
                        Repeat = repeat,
                        Command = irCommand
                    }
                );
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
