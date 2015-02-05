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

namespace IoStorm
{
    public class RouteController : BaseDevice
    {
        [Serializable]
        protected class RouteInformation
        {
            public string DestinationInstanceId { get; set; }

            public List<string> Payloads { get; set; }
        }

        private ILog log;
        private IHub hub;
        private Dictionary<string, RouteInformation> routes;

        public RouteController(ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.log = logFactory.GetLogger("RouteController");
            this.hub = hub;

            try
            {
                this.routes = BinaryRage.DB.Get<Dictionary<string, RouteInformation>>("Routing", this.hub.ConfigPath);
            }
            catch (DirectoryNotFoundException)
            {
                this.routes = new Dictionary<string, RouteInformation>();
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
                foreach (string instanceId in payload.IncomingInstanceId.Split(','))
                {
                    this.routes[instanceId.Trim()] = routeInfo;
                }

                BinaryRage.DB.Insert("Routing", this.routes, this.hub.ConfigPath);
            }
        }

        public void Incoming(Payload.Activity.ClearRoute payload, InvokeContext invCtx)
        {
            lock (this.routes)
            {
                if (this.routes.ContainsKey(payload.IncomingInstanceId))
                    this.routes.Remove(payload.IncomingInstanceId);
            }
        }

        public void Incoming(Payload.IPayload payload, InvokeContext invCtx)
        {
            string payloadType = payload.GetType().FullName;

            lock (this.routes)
            {
                RouteInformation routeInfo;

                if (this.routes.TryGetValue(invCtx.OriginDeviceId, out routeInfo))
                {
                    foreach (string routedPayload in routeInfo.Payloads)
                    {
                        if (payloadType.StartsWith(routedPayload))
                        {
                            // Match
                            this.hub.SendPayload(this.InstanceId, payload,
                                destinationInstanceId: routeInfo.DestinationInstanceId);
                        }
                    }
                }
            }
        }
    }
}
