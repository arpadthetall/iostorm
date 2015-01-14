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

namespace IoStorm
{
    /// <summary>
    /// Serial manager for CR-ended responses (LF is ignored) with timeout on partial lines
    /// </summary>
    internal class SerialLineManager : SerialManager
    {
        private StringBuilder buffer;
        private ISubject<string> lineReceived;
        private DateTime lastReceived;
        private TimeSpan timeout;

        public SerialLineManager(
            ILogFactory logFactory,
            string serialPortName,
            int baudRate,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
            : base(logFactory, serialPortName, baudRate, parity, dataBits, stopBits)
        {
            this.buffer = new StringBuilder();
            this.lineReceived = new Subject<string>();
            this.timeout = TimeSpan.FromSeconds(2);

            DeviceInitialized.Subscribe(x =>
                {
                    if (x)
                    {
                        // Reset buffer
                        this.buffer.Clear();
                    }
                });

            DataReceived.Subscribe(x =>
                {
                    WriteNewData((char)x);
                });
        }

        public TimeSpan Timeout
        {
            get { return this.timeout; }
            set { this.timeout = value; }
        }

        public IObservable<string> LineReceived
        {
            get { return this.lineReceived.AsObservable(); }
        }

        private void CheckTimeout()
        {
            DateTime now = DateTime.Now;

            if (this.buffer.Length > 0 && (now - this.lastReceived) > this.timeout)
            {
                // Timeout
                this.buffer.Clear();
            }

            this.lastReceived = now;
        }

        internal void WriteNewData(char value)
        {
            CheckTimeout();

            switch (value)
            {
                case '\n':
                    // Ignore
                    break;

                case '\r':
                    string lineData = buffer.ToString();
                    buffer.Clear();

                    if (!string.IsNullOrEmpty(lineData))
                        this.lineReceived.OnNext(lineData);
                    break;

                default:
                    buffer.Append(value);
                    break;
            }
        }
    }
}
