using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;
using Qlue.Logging;

namespace Storm.Plugins
{
    public class YamahaReceiver : BaseDevice
    {
        private ILog log;
        private IHub hub;
        private double currentVolume;

        public YamahaReceiver(ILogFactory logFactory, IHub hub)
        {
            this.log = logFactory.GetLogger("YamahaReceiver");
            this.hub = hub;
        }

        public void Incoming(Payload.Audio.ChangeVolume payload)
        {
            this.currentVolume += payload.Steps / 100;

            if (this.currentVolume < 0)
                this.currentVolume = 0;
            if (this.currentVolume > 1)
                this.currentVolume = 1;

            this.log.Info("Current volume: {0:P}", this.currentVolume);
        }

        public void Incoming(Payload.Audio.SetVolume payload)
        {
            this.currentVolume += payload.Volume;

            if (this.currentVolume < 0)
                this.currentVolume = 0;
            if (this.currentVolume > 1)
                this.currentVolume = 1;

            this.log.Info("Set current volume: {0:P}", this.currentVolume);
        }

        // Example
        //public void Incoming(Payload.IPayload payload)
        //{
        //    if (payload is Payload.Audio.ChangeVolume)
        //    {
        //        Incoming((Payload.Audio.ChangeVolume)payload);
        //    }
        //}
    }
}
