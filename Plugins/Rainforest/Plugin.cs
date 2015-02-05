using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Qlue.Logging;
using Newtonsoft.Json;
using System.IO;
using IoStorm.Plugin;
using Newtonsoft.Json.Linq;

namespace IoStorm.Plugins.Rainforest
{
    [Plugin(Name = "Rainforest Eagle", Description = "Rainforest Automation Eagle for Smart Meter Power consumption", Author = "IoStorm")]
    public class Plugin : BaseDevice
    {
        private ILog log;
        private IHub hub;
        private Timer pollTimer;
        private string eagleHostName;
        private string eagleMacId;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("Rainforest");

            this.eagleHostName = this.hub.GetSetting(this, "RainforestEagleHostName");
            if (string.IsNullOrEmpty(this.eagleHostName))
                throw new ArgumentException("Missing RainforestEagleHostName setting");

            var deviceList = JToken.Parse(RequestCommand("get_device_list"));

            var firstToken = deviceList["device_mac_id[0]"];
            if (firstToken == null)
                throw new ArgumentException("Invalid response from Eagle");

            this.eagleMacId = firstToken.Value<string>();

            this.pollTimer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(1));
        }

        private string RequestCommand(string command)
        {
            string pollUrl = string.Format("http://{0}/cgi-bin/cgi_manager", this.eagleHostName);

            var webRequest = (HttpWebRequest)HttpWebRequest.Create(pollUrl);

            webRequest.Method = "POST";
            webRequest.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;
            //            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ProtocolVersion = HttpVersion.Version10;
            WebResponse webResponse;
            using (var sw = new StreamWriter(webRequest.GetRequestStream()))
            {
                sw.NewLine = "\n";

                sw.WriteLine("<LocalCommand>");
                sw.WriteLine("<Name>{0}</Name>", command);
                if (!string.IsNullOrEmpty(this.eagleMacId))
                    sw.WriteLine("<MacId>{0}</MacId>", this.eagleMacId);
                sw.WriteLine("</LocalCommand>");
            }

            webResponse = webRequest.GetResponse();
            using (var sr = new StreamReader(webResponse.GetResponseStream()))
            {
                return sr.ReadToEnd();
            }
        }

        private void TimerCallback(object state)
        {
            lock (this)
            {
                if (string.IsNullOrEmpty(this.eagleMacId))
                    return;

                try
                {
                    var usageData = JsonConvert.DeserializeObject<UsageData>(RequestCommand("get_usage_data"));

                    var consumption = new Payload.Power.ConsumptionHistory();

                    switch (usageData.demand_units)
                    {
                        case "kW":
                            consumption.WattsNow = (long)(usageData.demand * 1000);
                            break;
                    }

                    switch (usageData.summation_units)
                    {
                        case "kWh":
                            consumption.TotalFromGridWattHours = (long)(usageData.summation_delivered * 1000);
                            consumption.TotalToGridWattHours = (long)(usageData.summation_received * 1000);
                            break;
                    }

                    this.hub.SendPayload(this, consumption);
                }
                catch (Exception ex)
                {
                    this.log.ErrorException("Exception in TimerCallback", ex);
                }
            }
        }
    }
}
