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

namespace IoStorm.Plugins.OscServer
{
    [Plugin(Name = "OSC Server", Description = "OSC Server", Author = "IoStorm")]
    public class Plugin : BaseDevice, IDisposable
    {
        private ILog log;
        private IHub hub;
        private Rug.Osc.OscReceiver receiver;
        private Task receiverTask;
        private System.Threading.CancellationTokenSource cancelSource;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("OscServer");

            int listenPort;
            int.TryParse(this.hub.GetSetting(this, "ListenPort"), out listenPort);
            if (listenPort == 0)
                throw new ArgumentException("Missing ListenPort setting");

            this.receiver = new Rug.Osc.OscReceiver(listenPort);
            this.cancelSource = new System.Threading.CancellationTokenSource();

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

            this.hub.BroadcastPayload(this, new Payload.OscMessage
                {
                    Address = message.Address,
                    Value = value
                });
        }

        public void Dispose()
        {
            this.cancelSource.Cancel();
            this.receiver.Close();
        }
    }
}
