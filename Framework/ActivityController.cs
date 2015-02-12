using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IoStorm.Addressing;
using Microsoft.Practices.Unity;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Qlue.Logging;

namespace IoStorm
{
    public class ActivityController : BasePlugin, IDisposable
    {
        protected class ActivityActions
        {
            public List<Action> SetupActions { get; set; }

            public List<Action> TeardownActions { get; set; }

            public List<Config.Route> Routes { get; set; }
        }

        [Serializable]
        protected class ActiveActivity
        {
            public string ActivityName { get; set; }

            public HashSet<InstanceAddress> RouteIncoming { get; set; }

            public ActiveActivity()
            {
                RouteIncoming = new HashSet<InstanceAddress>();
            }
        }

        private ILog log;
        private IHub hub;
        private Dictionary<ZoneAddress, Dictionary<string, ActivityActions>> activitiesPerZone;
        private Dictionary<ZoneAddress, ActiveActivity> currentActivityPerZone;
        private Queue<Action> executionQueue;
        private ManualResetEvent executionTrigger;
        private Task executionTask;
        private CancellationTokenSource cts;

        public ActivityController(ILogFactory logFactory, IHub hub, PluginAddress instanceId)
            : base(instanceId)
        {
            this.log = logFactory.GetLogger("ActivityController");
            this.hub = hub;

            this.activitiesPerZone = new Dictionary<ZoneAddress, Dictionary<string, ActivityActions>>();

            try
            {
                this.currentActivityPerZone = BinaryRage.DB.Get<Dictionary<ZoneAddress, ActiveActivity>>("CurrentActivity", this.hub.ConfigPath);
            }
            catch
            {
                this.currentActivityPerZone = new Dictionary<ZoneAddress, ActiveActivity>();

                BinaryRage.DB.Insert("CurrentActivity", this.currentActivityPerZone, this.hub.ConfigPath);
            }

            this.executionQueue = new Queue<Action>();
            this.executionTrigger = new ManualResetEvent(false);
            this.cts = new CancellationTokenSource();

            string content = File.ReadAllText("Config\\Activities.json");

            var loadedActivities = JsonConvert.DeserializeObject<List<Config.Activity>>(content);

            foreach (var loadedActivity in loadedActivities)
            {
                Dictionary<string, ActivityActions> activitiesInZone;
                if (!this.activitiesPerZone.TryGetValue(loadedActivity.ZoneId, out activitiesInZone))
                {
                    activitiesInZone = new Dictionary<string, ActivityActions>();
                    this.activitiesPerZone.Add(loadedActivity.ZoneId, activitiesInZone);
                }

                var actions = ExecuteSequence(loadedActivity.Setup);

                var activityActions = new ActivityActions
                {
                    SetupActions = actions,
                    Routes = loadedActivity.Routes
                };

                activitiesInZone.Add(loadedActivity.Name ?? string.Empty, activityActions);
            }

            this.executionTask = Task.Run(() =>
                {
                    while (true)
                    {
                        WaitHandle.WaitAny(new WaitHandle[] { this.cts.Token.WaitHandle, this.executionTrigger });

                        if (this.cts.IsCancellationRequested)
                            break;

                        this.executionTrigger.Reset();

                        while (this.executionQueue.Count > 0)
                        {
                            var action = this.executionQueue.Dequeue();

                            // Execute
                            try
                            {
                                action();
                            }
                            catch (Exception ex)
                            {
                                this.log.WarnException("Exception while executing action in execution queue", ex);
                            }
                        }
                    }
                });
        }

        private List<Action> ExecuteSequence(IEnumerable<JObject> input)
        {
            var list = new List<Action>();

            foreach (var item in input)
            {
                foreach (var sequenceItem in item)
                {
                    switch (sequenceItem.Key)
                    {
                        case "SendPayload":
                            list.Add(BuildActivitySendPayloadAction(sequenceItem.Value.ToObject<Config.ActivitySendPayload>()));
                            break;

                        case "Sleep":
                            list.Add(BuildActivitySleepAction(sequenceItem.Value.ToObject<Config.ActivitySleep>()));
                            break;

                        case "Comment":
                            break;

                        default:
                            this.log.Debug("Unknown sequence command {0}", sequenceItem.Key);
                            break;
                    }
                }
            }

            return list;
        }

        private Action BuildActivitySendPayloadAction(Config.ActivitySendPayload input)
        {
            string key = input.Payload;
            if (!key.StartsWith("IoStorm.Payload."))
                key = "IoStorm.Payload." + key;

            // Find type
            if (!key.Contains(","))
                // Create fully qualified name
                key = Assembly.CreateQualifiedName(typeof(Payload.IPayload).Assembly.FullName, key);

            var payloadType = Type.GetType(key);
            if (payloadType == null)
                throw new ArgumentException("Unknown payload type: " + key);

            var payload = input.Parameters.ToObject(payloadType);

            if (!(payload is Payload.IPayload))
                throw new ArgumentException("Payload is not inheriting from IPayload");

            return new Action(() =>
                {
                    this.hub.SendPayload(
                        originatingInstanceId: InstanceId,
                        destination: input.Destination,
                        payload: (Payload.IPayload)payload);
                });
        }

        private Action BuildActivitySleepAction(Config.ActivitySleep input)
        {
            if (input.Milliseconds <= 0)
                throw new ArgumentOutOfRangeException("Milliseconds");

            return new Action(() => System.Threading.Thread.Sleep(input.Milliseconds));
        }

        public void IncomingZone(Payload.Activity.SelectActivity payload, InvokeContext invCtx)
        {
            var destination = invCtx.Destination as ZoneAddress;
            if (destination == null)
            {
                this.log.Warn("Unknown zone");
                return;
            }

            this.log.Info("Select activity {0} in zone {1}", payload.ActivityName, destination);

            ActiveActivity activeActivity;

            lock (this.currentActivityPerZone)
            {
                ActiveActivity existingActivity;
                if (this.currentActivityPerZone.TryGetValue(destination, out existingActivity))
                {
                    if (existingActivity.RouteIncoming.Any())
                    {
                        this.hub.SendPayload(this, new Payload.Activity.ClearRoute
                        {
                            IncomingInstanceId = existingActivity.RouteIncoming.ToArray()
                        });
                    }

                    // Do something else with existing?
                }

                activeActivity = new ActiveActivity
                    {
                        ActivityName = payload.ActivityName
                    };
                this.currentActivityPerZone[destination] = activeActivity;

                BinaryRage.DB.Insert("CurrentActivity", this.currentActivityPerZone, this.hub.ConfigPath);
            }

            this.hub.SendPayload(this, new Payload.Activity.Feedback { CurrentActivityName = payload.ActivityName }, destination);

            Dictionary<string, ActivityActions> activitiesInZone;
            if (!this.activitiesPerZone.TryGetValue(destination, out activitiesInZone))
                return;

            ActivityActions activityActions;
            if (!activitiesInZone.TryGetValue(payload.ActivityName, out activityActions))
                return;

            lock (this.executionQueue)
            {
                if (activityActions.SetupActions != null)
                    foreach (var action in activityActions.SetupActions)
                        this.executionQueue.Enqueue(action);

                this.executionTrigger.Set();
            }

            if (activityActions.Routes != null)
            {
                foreach (var route in activityActions.Routes)
                {
                    route.Incoming.ForEach(x => activeActivity.RouteIncoming.Add(x));

                    this.hub.SendPayload(this, new Payload.Activity.SetRoute
                        {
                            IncomingInstanceId = route.Incoming.ToList(),
                            OutgoingInstanceId = route.Outgoing,
                            Payloads = route.Payloads
                        });
                }
            }
        }

        public void Dispose()
        {
            this.cts.Cancel();
            this.executionTask.Wait();
        }
    }
}
