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
            receiver.AddIrCommand("dokA/wAA", new NECx(0x89, 0x76, 0x00));

            // Vol +
            receiver.AddIrCommand("domAfwAA", new NECx(0x89, 0x76, 0x80));

            // Power Toggle
            receiver.AddIrCommand("dolAvwAA", new NECx(0x89, 0x76, 0x40));

            // Rewind
            receiver.AddIrCommand("donAPwAA", new NECx(0x89, 0x76, 0xc0));

            // Pause
            receiver.AddIrCommand("dokg3wAA", new NECx(0x89, 0x76, 0x20));

            // Fast Forward
            receiver.AddIrCommand("domgXwAA", new NECx(0x89, 0x76, 0xA0));

            // Add
            receiver.AddIrCommand("dolgnwAA", new NECx(0x89, 0x76, 0x60));

            // Navigation Up
            receiver.AddIrCommand("dongHwAA", new NECx(0x89, 0x76, 0xe0));

            // Play
            receiver.AddIrCommand("dokQ7wAA", new NECx(0x89, 0x76, 0x10));

            // Navigation Left
            receiver.AddIrCommand("domQbwAA", new NECx(0x89, 0x76, 0x90));

            // Navigation Right
            receiver.AddIrCommand("donQLwAA", new NECx(0x89, 0x76, 0xd0));

            // Navigation Down
            receiver.AddIrCommand("domwTwAA", new NECx(0x89, 0x76, 0xb0));

            // Numeric 1
            receiver.AddIrCommand("donwDwAA", new NECx(0x89, 0x76, 0xf0));

            // Numeric 2
            receiver.AddIrCommand("dokI9wAA", new NECx(0x89, 0x76, 0x08));

            // Numeric 3
            receiver.AddIrCommand("domIdwAA", new NECx(0x89, 0x76, 0x88));

            // Numeric 4
            receiver.AddIrCommand("dolItwAA", new NECx(0x89, 0x76, 0x48));

            // Numeric 5
            receiver.AddIrCommand("donINwAA", new NECx(0x89, 0x76, 0xc8));

            // Numeric 6
            receiver.AddIrCommand("doko1wAA", new NECx(0x89, 0x76, 0x28));

            // Numeric 7
            receiver.AddIrCommand("domoVwAA", new NECx(0x89, 0x76, 0xa8));

            // Numeric 8
            receiver.AddIrCommand("dololwAA", new NECx(0x89, 0x76, 0x68));

            // Numeric 9
            receiver.AddIrCommand("donoFwAA", new NECx(0x89, 0x76, 0xe8));

            // Numeric 0
            receiver.AddIrCommand("domYZwAA", new NECx(0x89, 0x76, 0x98));

            // Favorites
            receiver.AddIrCommand("dokY5wAA", new NECx(0x89, 0x76, 0x18));

            // Search
            receiver.AddIrCommand("dolYpwAA", new NECx(0x89, 0x76, 0x58));

            // Shuffle
            receiver.AddIrCommand("donYJwAA", new NECx(0x89, 0x76, 0xd8));

            // Repeat
            receiver.AddIrCommand("dok4xwAA", new NECx(0x89, 0x76, 0x38));

            // Sleep
            receiver.AddIrCommand("dom4RwAA", new NECx(0x89, 0x76, 0xb8));

            // Now Playing
            receiver.AddIrCommand("dol4hwAA", new NECx(0x89, 0x76, 0x78));

            // (Display) Size
            receiver.AddIrCommand("don4BwAA", new NECx(0x89, 0x76, 0xf8));

            // (Display) Brightness
            receiver.AddIrCommand("dokE+wAA", new NECx(0x89, 0x76, 0x04));
        }
    }
}
