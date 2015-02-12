using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Qlue.Logging;
using IoStorm.Addressing;

namespace IoStorm
{
    public class RouteController : BasePlugin
    {
        [Serializable]
        protected class RouteInformation
        {
            public StormAddress DestinationInstanceId { get; set; }

            public List<string> Payloads { get; set; }
        }

        private ILog log;
        private IHub hub;
        private Dictionary<InstanceAddress, RouteInformation> routes;

        public RouteController(ILogFactory logFactory, IHub hub, PluginAddress instanceId)
            : base(instanceId)
        {
            this.log = logFactory.GetLogger("RouteController");
            this.hub = hub;

            try
            {
                this.routes = BinaryRage.DB.Get<Dictionary<InstanceAddress, RouteInformation>>("Routing", this.hub.ConfigPath);
            }
            catch
            {
                this.routes = new Dictionary<InstanceAddress, RouteInformation>();

                BinaryRage.DB.Insert("Routing", this.routes, this.hub.ConfigPath);
            }
        }

        public void Incoming(Payload.Activity.SetRoute payload, InvokeContext invCtx)
        {
            var routeInfo = new RouteInformation
            {
                DestinationInstanceId = payload.OutgoingInstanceId,
                Payloads = payload.Payloads
            };

            lock (this.routes)
            {
                foreach (var instanceAddress in payload.IncomingInstanceId)
                {
                    this.routes[instanceAddress] = routeInfo;
                }

                BinaryRage.DB.Insert("Routing", this.routes, this.hub.ConfigPath);
            }
        }

        public void Incoming(Payload.Activity.ClearRoute payload, InvokeContext invCtx)
        {
            lock (this.routes)
            {
                foreach (var instanceAddress in payload.IncomingInstanceId)
                {
                    if (this.routes.ContainsKey(instanceAddress))
                        this.routes.Remove(instanceAddress);
                }
            }
        }

        public void IncomingZone(Payload.IPayload payload, InvokeContext invCtx)
        {
            string payloadType = payload.GetType().FullName;

            lock (this.routes)
            {
                RouteInformation routeInfo;

                if (this.routes.TryGetValue(invCtx.Originating, out routeInfo))
                {
                    foreach (string routedPayload in routeInfo.Payloads)
                    {
                        if (payloadType.StartsWith(routedPayload))
                        {
                            // Match
                            this.hub.SendPayload(this.InstanceId, payload,
                                destination: routeInfo.DestinationInstanceId);
                        }
                    }
                }
            }
        }
    }
}
