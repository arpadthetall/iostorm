//#define DEBUG_OSC

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;
using IoStorm.Plugin;
using System.IO;
using System.Net;
using Newtonsoft.Json;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace IoStorm.Plugins.OscServer
{
    [Plugin(Name = "OSC Server", Description = "OSC Server", Author = "IoStorm")]
    public class Plugin : BaseDevice, IDisposable
    {
        protected class ConnectedDevice : IDisposable
        {
            public DateTime LastCommunication { get; set; }

            public Rug.Osc.OscSender Sender { get; private set; }

            public ConnectedDevice(IPAddress ipAddress)
            {
                this.Sender = new Rug.Osc.OscSender(ipAddress, 9000);

                this.Sender.Connect();
            }

            public void Dispose()
            {
                this.Sender.Close();
                this.Sender.Dispose();
            }
        }

        private ILog log;
        private IHub hub;
        private Rug.Osc.OscReceiver receiver;
        private Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;
        private Dictionary<Tuple<string, string>, Action> mappedAddresses;
        private Dictionary<IPAddress, ConnectedDevice> connectedDevices;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("OscServer");

            int listenPort;
            int.TryParse(this.hub.GetSetting(this, "ListenPort", "8000"), out listenPort);
            if (listenPort == 0)
                throw new ArgumentException("Missing ListenPort setting");

            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();

            this.mappedAddresses = new Dictionary<Tuple<string, string>, Action>();
            this.connectedDevices = new Dictionary<IPAddress, ConnectedDevice>();

            string content = File.ReadAllText("Config\\Osc.json");

            var loadedRoutes = JsonConvert.DeserializeObject<List<Config.MatchMessage>>(content);

            foreach (var loadedRoute in loadedRoutes)
            {
                var key = Tuple.Create(loadedRoute.Address, loadedRoute.MatchValue);

                this.mappedAddresses[key] = BuildSendPayloadAction(loadedRoute.SendPayload);
            }

            this.receiverTask = new Task(x =>
            {
                try
                {
                    while (!this.cancelSource.IsCancellationRequested)
                    {
                        while (this.receiver.State != Rug.Osc.OscSocketState.Closed)
                        {
                            if (this.receiver.State == Rug.Osc.OscSocketState.Connected)
                            {
                                var packet = this.receiver.Receive();
#if DEBUG_OSC
                                log.Debug("Received OSC message: {0}", packet);
#endif

                                ConnectedDevice connectedDevice;
                                lock (this.connectedDevices)
                                {
                                    if (!this.connectedDevices.TryGetValue(packet.Origin.Address, out connectedDevice))
                                    {
                                        connectedDevice = new ConnectedDevice(packet.Origin.Address);

                                        this.connectedDevices.Add(packet.Origin.Address, connectedDevice);
                                    }
                                }

                                connectedDevice.LastCommunication = DateTime.Now;

                                if (packet is Rug.Osc.OscBundle)
                                {
                                    var bundles = (Rug.Osc.OscBundle)packet;
                                    if (bundles.Any())
                                    {
                                        foreach (var bundle in bundles)
                                        {
                                            var oscMessage = bundle as Rug.Osc.OscMessage;
                                            if (oscMessage != null)
                                            {
                                                Invoke(oscMessage);
                                            }
                                        }
                                    }
                                }

                                if (packet is Rug.Osc.OscMessage)
                                {
                                    var msg = (Rug.Osc.OscMessage)packet;
                                    Invoke(msg);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }, this.cancelSource.Token, TaskCreationOptions.LongRunning);

            this.receiverTask.Start();
            this.receiver.Connect();
        }

        private void Invoke(Rug.Osc.OscMessage message)
        {
            string value = null;

            if (message.Count > 0)
                value = string.Join(",", message);

            this.hub.SendPayload(this, new Payload.OscMessage
                {
                    Address = message.Address,
                    Value = value
                });

            Action action;
            if (this.mappedAddresses.TryGetValue(Tuple.Create(message.Address, value), out action))
            {
                action();
            }
        }

        private Action BuildSendPayloadAction(Config.SendPayload input)
        {
            string key = input.Payload;
            if (!key.StartsWith("IoStorm.Payload."))
                key = "IoStorm.Payload." + key;

            // Find type
            if (!key.Contains(","))
                // Create fully qualified name
                key = Assembly.CreateQualifiedName(typeof(Payload.IPayload).Assembly.FullName, key);

            var payloadType = Type.GetType(key);
            if (payloadType == null)
                throw new ArgumentException("Unknown payload type: " + key);

            object payload;
            if (input.Parameters == null)
                payload = new JObject().ToObject(payloadType);
            else
                payload = input.Parameters.ToObject(payloadType);

            if (!(payload is Payload.IPayload))
                throw new ArgumentException("Payload is not inheriting from IPayload");

            return new Action(() =>
            {
                this.hub.SendPayload(this, (Payload.IPayload)payload, destinationInstanceId: input.DestinationInstanceId, destinationZoneId: input.DestinationZoneId);
            });
        }

        public void Incoming(Payload.Activity.Feedback payload, InvokeContext ctx)
        {
            lock (this.connectedDevices)
            {
                foreach (var connectedDevice in this.connectedDevices.Values)
                {
                    switch (payload.CurrentActivityName)
                    {
                        case "":
                            connectedDevice.Sender.Send(new Rug.Osc.OscMessage("/1/labelActivity", "Off"));
                            connectedDevice.Sender.Send(new Rug.Osc.OscMessage("/1/Activity-WatchTV", 0));
                            connectedDevice.Sender.Send(new Rug.Osc.OscMessage("/1/Activity-Off", 1));
                            break;

                        default:
                            connectedDevice.Sender.Send(new Rug.Osc.OscMessage("/1/labelActivity", payload.CurrentActivityName));
                            connectedDevice.Sender.Send(new Rug.Osc.OscMessage("/1/Activity-WatchTV", 1));
                            connectedDevice.Sender.Send(new Rug.Osc.OscMessage("/1/Activity-Off", 0));
                            break;
                    }

                }
            }
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
            this.receiver.Close();

            foreach (var connectedDevice in this.connectedDevices.Values)
                connectedDevice.Dispose();
        }
    }
}
