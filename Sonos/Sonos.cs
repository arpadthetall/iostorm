using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;

namespace Storm.Sonos
{
    public class Sonos : BaseDevice
    {
        private Qlue.Logging.ILog log;
        private IHub hub;
        private SonosDiscovery discovery;

        public Sonos(Qlue.Logging.ILogFactory logFactory, IHub hub)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("Sonos");

            this.discovery = new Storm.Sonos.SonosDiscovery(logFactory);
            discovery.TopologyChanged += discovery_TopologyChanged;

            this.discovery.StartScan();
        }

        private void discovery_TopologyChanged()
        {
        }

        public void Incoming(Payload.Transport.Play payload)
        {
            var player = this.discovery.Players.FirstOrDefault();

            if (player != null)
            {
                this.log.Info("Playing player {0}", player.Name);

                player.Play();
            }
        }

        public void Incoming(Payload.Transport.Pause payload)
        {
            var player = this.discovery.Players.FirstOrDefault();

            if (player != null)
            {
                this.log.Info("Pausing player {0}", player.Name);

                player.Pause();
            }
        }
    }
}
