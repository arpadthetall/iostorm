using System;
using System.Reactive;
using System.Reactive.Subjects;
using IoStorm.IRProtocol;

namespace IoStorm.CorePlugins.RemoteMapping
{
    public static class IrManSamsung
    {
        // At some point these files should probably be external to the framework

        public static void MapRemoteControl(IrManReceiver receiver)
        {
            // RedA
            receiver.AddIrCommand("4OA2yQAA", new NECx(0x89, 0x76, 0xB0));

            // GreenB
            receiver.AddIrCommand("4OAo1wAA", new NECx(0x89, 0x76, 0xE0));
        }
    }
}
