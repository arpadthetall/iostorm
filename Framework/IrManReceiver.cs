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

namespace Storm
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

        public IrmanReceiver(Qlue.Logging.ILogFactory logFactory, IHub hub, string serialPortName)
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
                                IrReceived(Convert.ToBase64String(this.receiveBuffer));
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

        public void AddIrCommand(string data, Func<Payload.IPayload> payloadFunc)
        {
            this.irCommands[data] = payloadFunc;
        }

        private void IrReceived(string data)
        {
            Func<Payload.IPayload> func;
            if (this.irCommands.TryGetValue(data, out func))
            {
                var payload = func();

                this.hub.BroadcastPayload(this, payload);
            }
            else
                this.log.Debug("Unknown IR command {0}", data);
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
