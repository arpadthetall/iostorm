using System;

namespace IoStorm.RemoteMapping
{
    [Obsolete]
    public static class RemoteControlSB
    {
        /// <summary>
        /// Learned using an Irman and SlimDevices/Squeezebox remote (NEC2 protocol)
        /// </summary>
        /// <param name="receiver"></param>
        public static void MapRemoteControl(Plugins.IrmanReceiver receiver)
        {
            receiver.AddIrCommand("dokA/wAA", () => new Payload.Audio.VolumeDown());

            receiver.AddIrCommand("domAfwAA", () => new Payload.Audio.VolumeUp());

            receiver.AddIrCommand("dolAvwAA", () => new Payload.Power.Toggle());

            receiver.AddIrCommand("donAPwAA", () => new Payload.Transport.Rewind());

            receiver.AddIrCommand("dokg3wAA", () => new Payload.Transport.Pause());

            receiver.AddIrCommand("domgXwAA", () => new Payload.Transport.FastForward());

            receiver.AddIrCommand("dolgnwAA", () => new Payload.Navigation.Add());

            receiver.AddIrCommand("dokQ7wAA", () => new Payload.Transport.Play());

            receiver.AddIrCommand("dongHwAA", () => new Payload.Navigation.Up());

            receiver.AddIrCommand("domQbwAA", () => new Payload.Navigation.Left());

            receiver.AddIrCommand("donQLwAA", () => new Payload.Navigation.Right());

            receiver.AddIrCommand("domwTwAA", () => new Payload.Navigation.Down());

            receiver.AddIrCommand("donwDwAA", () => new Payload.Navigation.Number1());

            receiver.AddIrCommand("dokI9wAA", () => new Payload.Navigation.Number2());

            receiver.AddIrCommand("domIdwAA", () => new Payload.Navigation.Number3());

            receiver.AddIrCommand("dolItwAA", () => new Payload.Navigation.Number4());

            receiver.AddIrCommand("donINwAA", () => new Payload.Navigation.Number5());

            receiver.AddIrCommand("doko1wAA", () => new Payload.Navigation.Number6());

            receiver.AddIrCommand("domoVwAA", () => new Payload.Navigation.Number7());

            receiver.AddIrCommand("dololwAA", () => new Payload.Navigation.Number8());

            receiver.AddIrCommand("donoFwAA", () => new Payload.Navigation.Number9());

            receiver.AddIrCommand("domYZwAA", () => new Payload.Navigation.Number0());

            receiver.AddIrCommand("dokY5wAA", () => new Payload.Navigation.Favorites());

            receiver.AddIrCommand("dolYpwAA", () => new Payload.Navigation.Search());

            receiver.AddIrCommand("donYJwAA", () => new Payload.Transport.Shuffle());

            receiver.AddIrCommand("dok4xwAA", () => new Payload.Transport.Repeat());

            receiver.AddIrCommand("dom4RwAA", () => new Payload.Power.Sleep());

            receiver.AddIrCommand("dol4hwAA", () => new Payload.Navigation.NowPlaying());

            receiver.AddIrCommand("don4BwAA", () => new Payload.Navigation.DisplaySize());

            receiver.AddIrCommand("dokE+wAA", () => new Payload.Navigation.DisplayBrightness());
        }
    }
}
