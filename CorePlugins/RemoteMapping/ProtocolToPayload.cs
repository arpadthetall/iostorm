using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Qlue.Logging;
using IoStorm.IRProtocol;

namespace IoStorm.CorePlugins.RemoteMapping
{
    public class ProtocolToPayload : BaseDevice
    {
        private ILog log;
        private IHub hub;
        private Dictionary<Payload.IIRProtocol, Func<Payload.IPayload>> irToPayload;

        public ProtocolToPayload(ILogFactory logFactory, IHub hub)
        {
            this.log = logFactory.GetLogger("ProtocolToPayload");
            this.hub = hub;

            this.irToPayload = new Dictionary<Payload.IIRProtocol, Func<Payload.IPayload>>();
        }

        public void Incoming(Payload.IRCommand payload)
        {
            Func<Payload.IPayload> payloadFunc;
            lock (this.irToPayload)
            {
                if (!this.irToPayload.TryGetValue(payload.Command, out payloadFunc))
                    return;
            }

            if (payloadFunc != null)
                this.hub.BroadcastPayload(this, payloadFunc());
        }

        public void Add(Payload.IIRProtocol command, Func<Payload.IPayload> payloadFunc)
        {
            lock (this.irToPayload)
            {
                this.irToPayload[command] = payloadFunc;
            }
        }

        public void MapSqueezeBoxRemote()
        {
            // We should store this somewhere external

            Add(new NEC2(0x7689, 0x00ff), () => new Payload.Audio.VolumeDown());

            Add(new NEC2(0x7689, 0x807f), () => new Payload.Audio.VolumeUp());

            Add(new NEC2(0x7689, 0x40bf), () => new Payload.Power.Toggle());

            Add(new NEC2(0x7689, 0xc03f), () => new Payload.Transport.Rewind());

            Add(new NEC2(0x7689, 0x20df), () => new Payload.Transport.Pause());

            Add(new NEC2(0x7689, 0xa05f), () => new Payload.Transport.FastForward());

            Add(new NEC2(0x7689, 0x609f), () => new Payload.Navigation.Add());

            Add(new NEC2(0x7689, 0xe01f), () => new Payload.Navigation.Up());

            Add(new NEC2(0x7689, 0x10ef), () => new Payload.Transport.Play());

            Add(new NEC2(0x7689, 0x906f), () => new Payload.Navigation.Left());

            Add(new NEC2(0x7689, 0xd02f), () => new Payload.Navigation.Right());

            Add(new NEC2(0x7689, 0xb04f), () => new Payload.Navigation.Down());

            Add(new NEC2(0x7689, 0xf00f), () => new Payload.Navigation.Number1());

            Add(new NEC2(0x7689, 0x08f7), () => new Payload.Navigation.Number2());

            Add(new NEC2(0x7689, 0x8877), () => new Payload.Navigation.Number3());

            Add(new NEC2(0x7689, 0x48b7), () => new Payload.Navigation.Number4());

            Add(new NEC2(0x7689, 0xc837), () => new Payload.Navigation.Number5());

            Add(new NEC2(0x7689, 0x28d7), () => new Payload.Navigation.Number6());

            Add(new NEC2(0x7689, 0xa857), () => new Payload.Navigation.Number7());

            Add(new NEC2(0x7689, 0x6897), () => new Payload.Navigation.Number8());

            Add(new NEC2(0x7689, 0xe817), () => new Payload.Navigation.Number9());

            Add(new NEC2(0x7689, 0x18e7), () => new Payload.Navigation.Favorites());

            Add(new NEC2(0x7689, 0x9867), () => new Payload.Navigation.Number0());

            Add(new NEC2(0x7689, 0x58a7), () => new Payload.Navigation.Search());

            Add(new NEC2(0x7689, 0xd827), () => new Payload.Transport.Shuffle());

            Add(new NEC2(0x7689, 0x38c7), () => new Payload.Transport.Repeat());

            Add(new NEC2(0x7689, 0xb847), () => new Payload.Power.Sleep());

            Add(new NEC2(0x7689, 0x7887), () => new Payload.Navigation.NowPlaying());

            Add(new NEC2(0x7689, 0xf807), () => new Payload.Navigation.DisplaySize());

            Add(new NEC2(0x7689, 0x04fb), () => new Payload.Navigation.DisplayBrightness());
        }

        public void MapSonyTVRemoteRMYD024()
        {
            // We should store this somewhere external

            Add(new Sony12(1, 58), () => new Payload.TV.Display());

            Add(new Sony12(1, 21), () => new Payload.Power.Toggle());

            Add(new Sony15(151, 60), () => new Payload.Transport.Previous());

            Add(new Sony15(151, 121), () => new Payload.Transport.Replay());

            Add(new Sony15(151, 120), () => new Payload.Transport.Advance());

            Add(new Sony15(151, 61), () => new Payload.Transport.Next());

            Add(new Sony15(151, 27), () => new Payload.Transport.Rewind());

            Add(new Sony15(151, 26), () => new Payload.Transport.Play());

            Add(new Sony15(151, 28), () => new Payload.Transport.FastForward());

            Add(new Sony15(26, 88), () => new Payload.Navigation.SyncMenu());

            Add(new Sony15(151, 25), () => new Payload.Transport.Pause());

            Add(new Sony15(151, 24), () => new Payload.Transport.Stop());

            Add(new Sony15(119, 96), () => new Payload.Audio.Theater());

            Add(new Sony15(151, 123), () => new Payload.Audio.Sound());

            Add(new Sony12(1, 100), () => new Payload.TV.Picture());

            Add(new Sony15(164, 61), () => new Payload.TV.Wide());

            Add(new Sony15(26, 107), () => new Payload.Navigation.DMeX());

            Add(new Sony15(164, 16), () => new Payload.TV.CC());

            Add(new Sony12(1, 92), () => new Payload.TV.Freeze());

            Add(new Sony12(1, 14), () => new Payload.Navigation.Guide());

            Add(new Sony15(119, 118), () => new Payload.Navigation.Favorites());

            Add(new Sony12(1, 37), () => new Payload.TV.Input());

            Add(new Sony15(151, 35), () => new Payload.Navigation.Return());

            Add(new Sony12(1, 96), () => new Payload.Navigation.Home());

            Add(new Sony15(151, 54), () => new Payload.Navigation.Options());

            Add(new Sony12(1, 0), () => new Payload.Navigation.Number1());

            Add(new Sony12(1, 1), () => new Payload.Navigation.Number2());

            Add(new Sony12(1, 2), () => new Payload.Navigation.Number3());

            Add(new Sony12(1, 3), () => new Payload.Navigation.Number4());

            Add(new Sony12(1, 4), () => new Payload.Navigation.Number5());

            Add(new Sony12(1, 5), () => new Payload.Navigation.Number6());

            Add(new Sony12(1, 6), () => new Payload.Navigation.Number7());

            Add(new Sony12(1, 7), () => new Payload.Navigation.Number8());

            Add(new Sony12(1, 8), () => new Payload.Navigation.Number9());

            Add(new Sony12(1, 9), () => new Payload.Navigation.Number0());

            Add(new Sony15(151, 29), () => new Payload.Navigation.Period());

            Add(new Sony12(1, 11), () => new Payload.Navigation.Enter());

            Add(new Sony12(1, 16), () => new Payload.TV.ChannelInc());

            Add(new Sony12(1, 17), () => new Payload.TV.ChannelDec());

            Add(new Sony12(1, 18), () => new Payload.Audio.VolumeUp());

            Add(new Sony12(1, 19), () => new Payload.Audio.VolumeDown());

            Add(new Sony12(1, 20), () => new Payload.Audio.MuteToggle());

            Add(new Sony12(1, 59), () => new Payload.Navigation.Jump());

            Add(new Sony12(1, 101), () => new Payload.Navigation.Select());

            Add(new Sony12(1, 49), () => new Payload.Navigation.Up());

            Add(new Sony12(1, 52), () => new Payload.Navigation.Left());

            Add(new Sony12(1, 50), () => new Payload.Navigation.Down());

            Add(new Sony12(1, 51), () => new Payload.Navigation.Right());
        }
    }
}
