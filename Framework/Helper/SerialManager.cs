using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO.Ports;
using Qlue.Logging;

namespace IoStorm
{
    public class SerialManager : IDisposable
    {
        protected Qlue.Logging.ILog log;
        private CancellationTokenSource cts;
        private SerialPort serialPort;
        private ISubject<byte> dataReceived;
        private ISubject<bool> portAvailable;
        private ISubject<bool> deviceInitialized;
        private bool portOpened;
        private bool isDeviceInitialized;
        private TimeSpan deviceInitializationTimeout;

        public SerialManager(
            ILogFactory logFactory,
            string serialPortName,
            int baudRate,
            Parity parity = Parity.None,
            int dataBits = 8,
            StopBits stopBits = StopBits.One)
        {
            this.log = logFactory.GetLogger("SerialManager");

            this.serialPort = new SerialPort(serialPortName, baudRate, parity, dataBits, stopBits);
            this.serialPort.ReadTimeout = 1000;

            this.deviceInitializationTimeout = TimeSpan.FromSeconds(5);

            this.cts = new CancellationTokenSource();

            this.dataReceived = new Subject<byte>();
            this.portAvailable = new Subject<bool>();
            this.deviceInitialized = new Subject<bool>();
        }

        public void Start()
        {
            Task.Factory.StartNew(() => PortMonitor(), TaskCreationOptions.LongRunning);
        }

        public IObservable<byte> DataReceived
        {
            get { return this.dataReceived.AsObservable(); }
        }

        public IObservable<bool> PortAvailable
        {
            get { return this.portAvailable.AsObservable(); }
        }

        public IObservable<bool> DeviceInitialized
        {
            get { return this.deviceInitialized.AsObservable(); }
        }

        private void Close()
        {
            try
            {
                this.serialPort.Close();
            }
            catch (IOException)
            {
            }
        }

        public bool RtsEnable
        {
            get { return this.serialPort.RtsEnable; }
            set
            {
                try
                {
                    this.serialPort.RtsEnable = value;
                }
                catch (IOException)
                {
                    Close();
                }
            }
        }

        public bool DtrEnable
        {
            get { return this.serialPort.DtrEnable; }
            set
            {
                try
                {
                    this.serialPort.DtrEnable = value;
                }
                catch (IOException)
                {
                    Close();
                }
            }
        }

        public bool IsOpen
        {
            get { return this.serialPort.IsOpen; }
        }

        public bool IsDeviceInitialized
        {
            get { return this.isDeviceInitialized; }
            set
            {
                this.isDeviceInitialized = value;

                this.deviceInitialized.OnNext(value);
            }
        }

        public void Write(string data)
        {
            this.log.Trace("Send {0}", data);

            this.serialPort.Write(data);
        }

        private void PortMonitor()
        {
            this.log.Debug("PortMonitor running");

            while (!this.cts.IsCancellationRequested)
            {
                try
                {
                    IsDeviceInitialized = false;

                    while (!this.serialPort.IsOpen)
                    {
                        if (this.portOpened)
                        {
                            this.log.Debug("Port {0} closed", this.serialPort.PortName);

                            this.portOpened = false;
                            this.portAvailable.OnNext(false);
                        }

                        try
                        {
                            this.log.Debug("Attempting to open port {0} at {1:N0} bps", this.serialPort.PortName, this.serialPort.BaudRate);

                            this.serialPort.Open();

                            this.log.Debug("Port {0} opened", this.serialPort.PortName);

                            this.portOpened = true;
                            this.portAvailable.OnNext(true);
                        }
                        catch (IOException ex)
                        {
                            this.log.Debug("Unable to open port {0}, error {1}", this.serialPort.PortName, ex.Message);

                            this.cts.Token.WaitHandle.WaitOne(10000);
                            break;
                        }
                    }

                    DateTime portOpened = DateTime.Now;

                    while (this.serialPort != null && this.serialPort.IsOpen)
                    {
                        try
                        {
                            byte b = (byte)this.serialPort.ReadByte();

                            this.dataReceived.OnNext(b);
                        }
                        catch (IOException)
                        {
                            break;
                        }
                        catch (TimeoutException)
                        {
                        }

                        if (!IsDeviceInitialized && (DateTime.Now - portOpened) > this.deviceInitializationTimeout)
                        {
                            this.log.Warn("Device is not initialized, attempt reset");

                            // Cycle the port available subscription
                            this.portAvailable.OnNext(false);
                            this.portAvailable.OnNext(true);

                            portOpened = DateTime.Now;
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.log.WarnException(ex, "Exception in PortMonitor");

                    Close();
                }
            }

            this.log.Debug("PortMonitor closing");
        }

        public void Dispose()
        {
            this.log.Trace("Disposing");

            this.cts.Cancel();

            if (this.serialPort != null)
            {
                try
                {
                    if (this.serialPort.IsOpen)
                        this.serialPort.Close();
                }
                catch
                {
                }

                this.serialPort.Dispose();

                this.serialPort = null;
            }
        }
    }
}
