using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.IO.Ports;

namespace Storm.Plugins
{
    public static class RemoteControlYamahaReceiver
    {
        public static void MapRemoteControl(IrmanReceiver receiver)
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
