using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace IoStorm.Sample2
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource cts;
        private Task amqpReceivingTask;
        private RemoteHub remoteHub;
        private IObservable<Tuple<Payload.IPayload, InvokeContext>> incomingMessages;

        public Form1(RemoteHub remoteHub)
        {
            this.remoteHub = remoteHub;
            InitializeComponent();

            this.cts = new CancellationTokenSource();

            var incomingSubject = new Subject<Tuple<Payload.IPayload, InvokeContext>>();

            this.amqpReceivingTask = Task.Run(() =>
            {
                this.remoteHub.Receiver(cts.Token, incomingSubject.AsObserver());
            }, cts.Token);

            this.incomingMessages = incomingSubject.AsObservable();

            this.incomingMessages
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(x =>
                {
                    listBox1.Items.Add(string.Format("Received {1} from {0}", x.Item2.Originating, x.Item1.GetType().Name));
                });
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            this.cts.Cancel();

            try
            {
                this.amqpReceivingTask.Wait();
            }
            catch
            {
            }
        }

        private void listBox1_DoubleClick(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        private void buttonOn_Click(object sender, EventArgs e)
        {
            //remoteHub.SendPayload("Global", new Payload.Light.On
            //    {
            //        LightId = textBoxDeviceId.Text
            //    });

            remoteHub.SendPayload(new Payload.Power.Set
                {
                    Value = true
                });
        }

        private void buttonOff_Click(object sender, EventArgs e)
        {
            remoteHub.SendPayload(new Payload.Light.Off
            {
                LightId = textBoxDeviceId.Text
            });
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // List zones

            var response = remoteHub.SendRpc<Payload.Management.ListZonesResponse>(new Payload.Management.ListZonesRequest(), TimeSpan.FromSeconds(10));

            foreach (var item in response.Zones)
            {
                listBox1.Items.Add(string.Format("Zone {0}   {1}", item.ZoneId, item.Name));
            }
        }
    }
}
