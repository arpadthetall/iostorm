using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO.Ports;
using Qlue.Logging;

namespace IoStorm.CorePlugins
{
    public class IrmanReceiver : BaseDevice, IDisposable
    {
        public enum States
        {
            WaitingForO,
            WaitingForK,
            Running
        }

        private Qlue.Logging.ILog log;
        private IHub hub;
        private SerialFixedManager serialManager;
        private States state;
        private Dictionary<string, Func<Payload.IPayload>> irCommands;
        private string lastReceivedData;
        private DateTime lastReceivedStartOfIR;
        private DateTime lastReceivedIR;
        private int repeated;

        public IrmanReceiver(ILogFactory logFactory, IHub hub, string serialPortName)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("IrmanReceiver");

            this.serialManager = new SerialFixedManager(logFactory, serialPortName, 9600, 6);
            this.irCommands = new Dictionary<string, Func<Payload.IPayload>>();

            this.serialManager.PortAvailable.Subscribe(isOpen =>
                {
                    if (isOpen)
                    {
                        Reset();
                    }
                });

            this.serialManager.DataReceived
                .Subscribe(data =>
                {
                    if (!this.serialManager.IsDeviceInitialized)
                    {
                        switch (this.state)
                        {
                            case States.WaitingForO:
                                this.log.Trace("Received {0} (state {1})", data, this.state);
                                if (data == 'O')
                                    this.state = States.WaitingForK;
                                break;

                            case States.WaitingForK:
                                this.log.Trace("Received {0} (state {1})", data, this.state);
                                if (data == 'K')
                                {
                                    this.state = States.Running;
                                    this.serialManager.IsDeviceInitialized = true;
                                }
                                else
                                    this.state = States.WaitingForO;
                                break;
                        }
                    }
                });

            this.serialManager.PacketReceived.Subscribe(IrReceived);

            this.serialManager.Start();
        }

        private void Reset()
        {
            this.serialManager.IsDeviceInitialized = false;

            this.serialManager.RtsEnable = false;
            this.serialManager.DtrEnable = false;

            Thread.Sleep(100);

            this.serialManager.RtsEnable = true;
            this.serialManager.DtrEnable = true;

            Thread.Sleep(100);

            this.state = States.WaitingForO;

            this.serialManager.Write("I");
            Thread.Sleep(1);
            this.serialManager.Write("R");
        }

        public void AddIrCommand(string data, Payload.IIRProtocol command)
        {
            AddIrCommand(data, () => new Payload.IRCommand
            {
                Command = command
            });
        }

        public void AddIrCommand(string data, Func<Payload.IPayload> payloadFunc)
        {
            this.irCommands[data] = payloadFunc;
        }

        private void IrReceived(byte[] data)
        {
            string rawData = Convert.ToBase64String(data);

            // Reset bit 0 for IR that does bit toggle
            data[0] &= 0xfe;
            string toggleData = Convert.ToBase64String(data);

            Func<Payload.IPayload> func;
            string irData = null;
            if (this.irCommands.TryGetValue(rawData, out func))
            {
                irData = rawData;
            }
            else if (this.irCommands.TryGetValue(toggleData, out func))
            {
                irData = toggleData;
            }

            if (func != null)
            {
                // Check repeat data
                DateTime now = DateTime.Now;
                var sinceLast = now - this.lastReceivedIR;
                this.lastReceivedIR = now;

                if (sinceLast.TotalMilliseconds > 400)
                    this.lastReceivedData = null;

                if (this.lastReceivedData == irData)
                {
                    // Repeat
                    var sinceFirst = now - this.lastReceivedStartOfIR;

                    if (this.repeated == 0 && sinceFirst.TotalMilliseconds < 300)
                        // Ignore first repeat
                        return;

                    this.repeated++;
                }
                else
                {
                    this.repeated = 0;
                    this.lastReceivedData = irData;
                    this.lastReceivedStartOfIR = DateTime.Now;
                }

                var payload = func();

                var irPayload = payload as Payload.IRCommand;
                if (irPayload != null)
                {
                    irPayload.Repeat = this.repeated;
                }

                this.hub.BroadcastPayload(this, payload);
            }
            else
                this.log.Debug("Unknown IR command {0} (tgl: {1})", rawData, toggleData);
        }

        public void Dispose()
        {
            this.serialManager.Dispose();
        }
    }
}
