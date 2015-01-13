using System;
using System.Reactive;
using System.Reactive.Subjects;
using Storm.RemoteMapping.IRProtocol;

namespace Storm.RemoteMapping
{
    public static class IrManSony12
    {
        private static void Translate(Plugins.IrmanReceiver receiver, string irManData, Payload.IIRProtocol command)
        {
            receiver.AddIrCommand(irManData, () => new Payload.IRCommand
                {
                    Command = command
                });
        }

        public static void MapRemoteControl(Plugins.IrmanReceiver receiver)
        {
            // Numeric 1
            Translate(receiver, "AQAAAAAA", new Sony12(1, 0));

            // Numeric 2
            Translate(receiver, "gQAAAAAA", new Sony12(1, 1));

            // Numeric 3
            Translate(receiver, "QQAAAAAA", new Sony12(1, 2));

            // Numeric 4
            Translate(receiver, "wQAAAAAA", new Sony12(1, 3));

            // Numeric 5
            Translate(receiver, "IQAAAAAA", new Sony12(1, 4));

            // Numeric 6
            Translate(receiver, "oQAAAAAA", new Sony12(1, 5));

            // Numeric 7
            Translate(receiver, "YQAAAAAA", new Sony12(1, 6));

            // Numeric 8
            Translate(receiver, "4QAAAAAA", new Sony12(1, 7));

            // Numeric 9
            Translate(receiver, "EQAAAAAA", new Sony12(1, 8));

            // Numeric 0
            Translate(receiver, "kQAAAAAA", new Sony12(1, 9));

            // Period
            Translate(receiver, "udIAAAAA", new Sony12(151, 29));

            // Enter
            Translate(receiver, "0QAAAAAA", new Sony12(1, 11));

            // Channel +
            Translate(receiver, "CQAAAAAA", new Sony12(1, 16));

            // Channel -
            Translate(receiver, "iQAAAAAA", new Sony12(1, 17));

            // Vol +
            Translate(receiver, "SQAAAAAA", new Sony12(1, 18));

            // Vol -
            Translate(receiver, "yQAAAAAA", new Sony12(1, 19));

            // Mute
            Translate(receiver, "KQAAAAAA", new Sony12(1, 20));

            // Jump
            Translate(receiver, "3QAAAAAA", new Sony12(1, 59));

            // Select
            Translate(receiver, "pwAAAAAA", new Sony12(1, 101));

            // Up
            Translate(receiver, "LwAAAAAA", new Sony12(1, 49));

            // Left
            Translate(receiver, "LQAAAAAA", new Sony12(1, 52));

            // Down
            Translate(receiver, "rwAAAAAA", new Sony12(1, 50));

            // Right
            Translate(receiver, "zQAAAAAA", new Sony12(1, 51));
        }
    }
}
