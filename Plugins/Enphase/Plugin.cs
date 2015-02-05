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

namespace IoStorm.Plugins.Enphase
{
    [Plugin(Name = "Enphase Gateway", Description = "Enphase Gateway for Solar Panels", Author = "IoStorm")]
    public class Plugin : BaseDevice
    {
        private ILog log;
        private IHub hub;
        private Timer pollTimer;
        private string enphaseHostName;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("Enphase");

            this.enphaseHostName = this.hub.GetSetting(this, "EnphaseHostName");
            if (string.IsNullOrEmpty(this.enphaseHostName))
                throw new ArgumentException("Missing EnphaseHostName setting");

            this.pollTimer = new Timer(TimerCallback, null, TimeSpan.FromSeconds(1), TimeSpan.FromMinutes(5));
        }

        private void TimerCallback(object state)
        {
            lock (this)
            {
                try
                {
                    string pollUrl = string.Format("http://{0}/api/v1/production", this.enphaseHostName);

                    var webRequest = HttpWebRequest.Create(pollUrl);

                    var webResponse = webRequest.GetResponse();

                    using (var sr = new StreamReader(webResponse.GetResponseStream()))
                    {
                        var productionData = JsonConvert.DeserializeObject<ProductionData>(sr.ReadToEnd());

                        this.log.Debug("Received production data from Enphase, watts = {0}", productionData.wattsNow);

                        var generationPayload = new Payload.Power.GenerationHistory
                        {
                            GenerationType = Payload.Power.GenerationTypes.Solar,
                            WattsNow = productionData.wattsNow,
                            WattHoursLifetime = productionData.wattHoursLifetime,
                            WattHoursSevenDays = productionData.wattHoursSevenDays,
                            WattHoursToday = productionData.wattHoursToday
                        };

                        this.hub.SendPayload(this, generationPayload);
                    }
                }
                catch (Exception ex)
                {
                    this.log.ErrorException("Exception in TimerCallback", ex);
                }
            }
        }
    }
}
