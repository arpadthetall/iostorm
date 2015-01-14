using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using OpenSource.UPnP;

namespace IoStorm.Sonos
{
    public class SonosDiscovery
    {
        private Qlue.Logging.ILogFactory logFactory;
        private Qlue.Logging.ILog log;
        private IList<SonosZone> zones = new List<SonosZone>();
        private IList<SonosPlayer> players = new List<SonosPlayer>();
        private UPnPSmartControlPoint controlPoint;

        private IDictionary<string, UPnPDevice> playerDevices = new Dictionary<string, UPnPDevice>();
        private Timer stateChangedTimer;

        public SonosDiscovery(Qlue.Logging.ILogFactory logFactory)
        {
            this.logFactory = logFactory;
            this.log = logFactory.GetLogger("SonosDiscovery");
        }

        public virtual void StartScan()
        {
            this.controlPoint = new UPnPSmartControlPoint(OnDeviceAdded, OnServiceAdded, "urn:schemas-upnp-org:device:ZonePlayer:1");
        }

        public IList<SonosPlayer> Players
        {
            get { return players; }
            set { players = value; }
        }

        public IList<SonosZone> Zones
        {
            get { return zones; }
            set { zones = value; }
        }

        public event Action TopologyChanged;

        private void OnServiceAdded(UPnPSmartControlPoint sender, UPnPService service)
        {
        }

        private void OnDeviceAdded(UPnPSmartControlPoint cp, UPnPDevice device)
        {
            this.log.Info("Found player {0}, id {1}", device.FriendlyName, device.UniqueDeviceName);

            // we need to save these for future reference
            lock (playerDevices)
            {
                playerDevices[device.UniqueDeviceName] = device;
            }

            // okay, we will try and notify the players that they have been found now.
            var player = players.FirstOrDefault(p => p.UUID == device.UniqueDeviceName);
            if (player != null)
            {
                player.SetDevice(device);
            }

            // Subscribe to events
            var topologyService = device.GetService("urn:upnp-org:serviceId:ZoneGroupTopology");
            topologyService.Subscribe(600, (service, subscribeOk) =>
                {
                    if (!subscribeOk)
                        return;

                    var stateVariable = service.GetStateVariableObject("ZoneGroupState");
                    stateVariable.OnModified += OnZoneGroupStateChanged;
                });
        }

        private void OnZoneGroupStateChanged(UPnPStateVariable sender, object newvalue)
        {
            //            this.log.Trace("OnZoneGroupStateChanged: {0}", sender.Value);

            // Avoid multiple state changes and consolidate them
            if (stateChangedTimer != null)
                stateChangedTimer.Dispose();
            stateChangedTimer = new Timer((state) => HandleZoneXML(sender.Value.ToString()), null, TimeSpan.FromMilliseconds(700),
                                          TimeSpan.FromMilliseconds(-1));
        }

        private void HandleZoneXML(string xml)
        {
            var doc = XElement.Parse(xml);
            lock (zones)
            {
                zones.Clear();
                foreach (var zoneXML in doc.Descendants("ZoneGroup"))
                {
                    CreateZone(zoneXML);
                }
            }

            lock (players)
            {
                players.Clear();
                lock (zones)
                {
                    players = zones.SelectMany(z => z.Players).ToList();
                }
            }

            if (TopologyChanged != null)
                TopologyChanged.Invoke();
        }

        private void CreateZone(XElement zoneXml)
        {
            var players = zoneXml.Descendants("ZoneGroupMember").Where(x => x.Attribute("Invisible") == null).ToList();
            if (players.Count > 0)
            {
                var zone = new SonosZone((string)zoneXml.Attribute("Coordinator"));

                foreach (var playerXml in players)
                {
                    var player = new SonosPlayer(
                        this.logFactory,
                        (string)playerXml.Attribute("ZoneName"),
                        (string)playerXml.Attribute("UUID"));

                    zone.AddPlayer(player);
                    Players.Add(player);

                    // This can happen before or after the topology event...
                    if (playerDevices.ContainsKey(player.UUID))
                    {
                        player.SetDevice(playerDevices[player.UUID]);
                    }
                }

                Zones.Add(zone);
            }
        }
    }
}