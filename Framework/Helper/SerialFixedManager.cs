using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO.Ports;
using Qlue.Logging;

namespace Storm
{
    /// <summary>
    /// Serial port manager for fixed packet sizes (manages timeout if partial packet received)
    /// </summary>
    internal class SerialFixedManager : SerialManager
    {
        private byte[] receiveBuffer;
        private int receiveCount;
        private ISubject<byte[]> packetReceived;
        private DateTime lastReceived;
        private TimeSpan timeout;

        public SerialFixedManager(
            ILogFactory logFactory,
            string serialPortName,
            int baudRate,
            int packetSize,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
            : base(logFactory, serialPortName, baudRate, parity, dataBits, stopBits)
        {
            this.timeout = TimeSpan.FromSeconds(2);
            this.receiveBuffer = new byte[packetSize];
            this.receiveCount = 0;
            this.packetReceived = new Subject<byte[]>();

            DeviceInitialized.Subscribe(x =>
                {
                    if (x)
                    {
                        // Reset buffer
                        this.receiveCount = 0;
                    }
                });

            DataReceived.Subscribe(data =>
                {
                    CheckTimeout();

                    if (this.receiveCount < packetSize)
                    {
                        this.receiveBuffer[this.receiveCount++] = data;
                    }

                    if (this.receiveCount == packetSize)
                    {
                        this.packetReceived.OnNext(this.receiveBuffer.ToArray());
                        this.receiveCount = 0;
                    }
                });
        }

        public TimeSpan Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        public IObservable<byte[]> PacketReceived
        {
            get { return this.packetReceived.AsObservable(); }
        }

        private void CheckTimeout()
        {
            DateTime now = DateTime.Now;

            if (this.receiveCount > 0 && (now - this.lastReceived) > this.timeout)
            {
                // Timeout
                this.receiveCount = 0;
            }

            this.lastReceived = now;
        }
    }
}
