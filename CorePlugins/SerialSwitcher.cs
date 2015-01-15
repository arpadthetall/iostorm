using System;
using System.Threading;
using Qlue.Logging;

namespace IoStorm.CorePlugins
{
    public class SerialSwitcher : BaseDevice, IDisposable
    {
        public enum States
        {
            WaitingForResponse,
            Running
        }

        private Qlue.Logging.ILog log;
        private IHub hub;
        private SerialLineManager serialManager;
        private States state;

        public SerialSwitcher(ILogFactory logFactory, IHub hub, string serialPortName, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("SerialSwitcher");

            this.serialManager = new SerialLineManager(logFactory, serialPortName, 9600);

            this.serialManager.PortAvailable.Subscribe(isOpen =>
                {
                    if (isOpen)
                    {
                        Reset();
                    }
                });

            this.serialManager.LineReceived
                .Subscribe(data =>
                {
                    if (!this.serialManager.IsDeviceInitialized)
                    {
                        switch (this.state)
                        {
                            case States.WaitingForResponse:
                                this.log.Trace("Received {0} (state {1})", data, this.state);

                                if (data.StartsWith("A") || data.StartsWith("V") || data.Contains(" A") || data.Contains(" V"))
                                {
                                    this.state = States.Running;
                                    this.serialManager.IsDeviceInitialized = true;
                                }

                                break;
                        }
                    }
                    else
                        this.log.Trace("Recevied {0}", data);
                });

            this.serialManager.Start();
        }

        private void Reset()
        {
            this.state = States.WaitingForResponse;
            this.serialManager.IsDeviceInitialized = false;

            this.serialManager.Write("I");
        }

        public void Incoming(Payload.Audio.SetInputOutput payload)
        {
            if (!this.serialManager.IsDeviceInitialized)
                return;

            this.serialManager.Write(string.Format("{0}*{1}!", payload.Input, payload.Output));
        }

        public void Dispose()
        {
            this.serialManager.Dispose();
        }
    }
}
