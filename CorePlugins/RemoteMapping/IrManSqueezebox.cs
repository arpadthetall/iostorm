using System;
using System.Reactive;
using System.Reactive.Subjects;
using IoStorm.IRProtocol;

namespace IoStorm.CorePlugins.RemoteMapping
{
    public static class IrManSqueezebox
    {
        // At some point these files should probably be external to the framework

        public static void MapRemoteControl(IoStorm.CorePlugins.IrManReceiver receiver)
        {
            // Vol -
            receiver.AddIrCommand("dokA/wAA", new NEC2(0x7689, 0x00ff));

            // Vol +
            receiver.AddIrCommand("domAfwAA", new NEC2(0x7689, 0x807f));

            // Power Toggle
            receiver.AddIrCommand("dolAvwAA", new NEC2(0x7689, 0x40bf));

            // Rewind
            receiver.AddIrCommand("donAPwAA", new NEC2(0x7689, 0xc03f));

            // Pause
            receiver.AddIrCommand("dokg3wAA", new NEC2(0x7689, 0x20df));

            // Fast Forward
            receiver.AddIrCommand("domgXwAA", new NEC2(0x7689, 0xa05f));

            // Add
            receiver.AddIrCommand("dolgnwAA", new NEC2(0x7689, 0x609f));

            // Navigation Up
            receiver.AddIrCommand("dongHwAA", new NEC2(0x7689, 0xe01f));

            // Play
            receiver.AddIrCommand("dokQ7wAA", new NEC2(0x7689, 0x10ef));

            // Navigation Left
            receiver.AddIrCommand("domQbwAA", new NEC2(0x7689, 0x906f));

            // Navigation Right
            receiver.AddIrCommand("donQLwAA", new NEC2(0x7689, 0xd02f));

            // Navigation Down
            receiver.AddIrCommand("domwTwAA", new NEC2(0x7689, 0xb04f));

            // Numeric 1
            receiver.AddIrCommand("donwDwAA", new NEC2(0x7689, 0xf00f));

            // Numeric 2
            receiver.AddIrCommand("dokI9wAA", new NEC2(0x7689, 0x08f7));

            // Numeric 3
            receiver.AddIrCommand("domIdwAA", new NEC2(0x7689, 0x8877));

            // Numeric 4
            receiver.AddIrCommand("dolItwAA", new NEC2(0x7689, 0x48b7));

            // Numeric 5
            receiver.AddIrCommand("donINwAA", new NEC2(0x7689, 0xc837));

            // Numeric 6
            receiver.AddIrCommand("doko1wAA", new NEC2(0x7689, 0x28d7));

            // Numeric 7
            receiver.AddIrCommand("domoVwAA", new NEC2(0x7689, 0xa857));

            // Numeric 8
            receiver.AddIrCommand("dololwAA", new NEC2(0x7689, 0x6897));

            // Numeric 9
            receiver.AddIrCommand("donoFwAA", new NEC2(0x7689, 0xe817));

            // Numeric 0
            receiver.AddIrCommand("domYZwAA", new NEC2(0x7689, 0x9867));

            // Favorites
            receiver.AddIrCommand("dokY5wAA", new NEC2(0x7689, 0x18e7));

            // Search
            receiver.AddIrCommand("dolYpwAA", new NEC2(0x7689, 0x58a7));

            // Shuffle
            receiver.AddIrCommand("donYJwAA", new NEC2(0x7689, 0xd827));

            // Repeat
            receiver.AddIrCommand("dok4xwAA", new NEC2(0x7689, 0x38c7));

            // Sleep
            receiver.AddIrCommand("dom4RwAA", new NEC2(0x7689, 0xb847));

            // Now Playing
            receiver.AddIrCommand("dol4hwAA", new NEC2(0x7689, 0x7887));

            // (Display) Size
            receiver.AddIrCommand("don4BwAA", new NEC2(0x7689, 0xf807));

            // (Display) Brightness
            receiver.AddIrCommand("dokE+wAA", new NEC2(0x7689, 0x04fb));
        }
    }
}
