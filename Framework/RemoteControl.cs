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

namespace Storm
{
    public static class RemoteControl
    {
        public static void MapRemoteControl(IrmanReceiver receiver)
        {
            receiver.AddIrCommand("dokA/wAA", () => new Payload.Audio.ChangeVolume
                    {
                        Steps = -1
                    });

            receiver.AddIrCommand("domAfwAA", () => new Payload.Audio.ChangeVolume
                    {
                        Steps = 1
                    });

            receiver.AddIrCommand("dokg3wAA", () => new Payload.Transport.PauseTransport());
        }
    }
}
