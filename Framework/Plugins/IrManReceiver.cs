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

namespace Storm.Plugins
{
    public class IrmanReceiver : BaseDevice, IDisposable
    {
        public enum States
        {
            NotInitialized,
            WaitingForO,
            WaitingForK,
            Running
        }

        private const int PacketSize = 6;

        private Qlue.Logging.ILog log;
        private IHub hub;
        private SerialManager serialManager;
        private States state;
        private DateTime lastReceivedByte;
        private byte[] receiveBuffer;
        private int receiveCount;
        private Dictionary<string, Func<Payload.IPayload>> irCommands;
        private Timer keepAliveTimer;
        private string lastReceivedData;
        private DateTime lastReceivedStartOfIR;
        private DateTime lastReceivedIR;
        private int repeated;

        public IrmanReceiver(ILogFactory logFactory, IHub hub, string serialPortName)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("IrmanReceiver");

            this.serialManager = new SerialManager(logFactory, serialPortName, 9600);
            this.lastReceivedByte = DateTime.MinValue;
            this.receiveBuffer = new byte[PacketSize];
            this.receiveCount = 0;
            this.irCommands = new Dictionary<string, Func<Payload.IPayload>>();

            this.keepAliveTimer = new Timer(x =>
            {
                try
                {
                    if (this.serialManager.IsOpen && this.state != States.Running)
                        Reset();
                }
                catch (Exception ex)
                {
                    this.log.WarnException("Exception in KeepAliveTimer", ex);
                }
            }, null, 5000, 5000);

            this.serialManager.PortAvailable.Subscribe(isOpen =>
                {
                    if (isOpen)
                    {
                        Reset();
                    }
                    else
                        this.state = States.NotInitialized;
                });

            this.serialManager.DataReceived
                .Subscribe(data =>
                {
                    if (this.state == States.NotInitialized)
                        // Ignore
                        return;

                    if (this.state == States.Running &&
                        this.receiveCount > 0 &&
                        (DateTime.Now - this.lastReceivedByte).TotalMilliseconds > 100)
                    {
                        // Reset
                        this.receiveCount = 0;
                    }

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
                                this.state = States.Running;
                            else
                                this.state = States.WaitingForO;
                            break;

                        case States.Running:
                            if (this.receiveCount < PacketSize)
                            {
                                this.receiveBuffer[this.receiveCount++] = data;
                            }

                            if (this.receiveCount == PacketSize)
                            {
                                IrReceived(this.receiveBuffer);
                                this.receiveCount = 0;
                            }
                            break;
                    }

                    this.lastReceivedByte = DateTime.Now;
                });

            this.serialManager.Start();
        }

        private void Reset()
        {
            this.state = States.NotInitialized;

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
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.serialManager.Dispose();
            }
        }
    }
}
