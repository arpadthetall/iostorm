
using System;

namespace Storm.RemoteMapping
{
    public static class RemoteControlYamahaReceiver
    {
        public static void MapRemoteControl(Plugins.IrmanReceiver receiver)
        {
            receiver.AddIrCommand("XqHYJwAA", () => new Payload.Audio.ChangeVolume
                    {
                        Steps = -1
                    });

            receiver.AddIrCommand("XqFYpwAA", () => new Payload.Audio.ChangeVolume
                    {
                        Steps = 1
                    });

            receiver.AddIrCommand("PsFapQAA", () => new Payload.Transport.Pause());
        }
    }
}
