using System;
using System.Reactive;
using System.Reactive.Subjects;
using Storm.RemoteMapping.IRProtocol;

namespace Storm.RemoteMapping
{
    public static class IrManSony
    {
        // At some point these files should probably be external to the framework

        public static void MapRemoteControl(Plugins.IrmanReceiver receiver)
        {
            // Display
            receiver.AddIrCommand("XAAAAAAA", new Sony12(1, 58));

            // TV Power
            receiver.AddIrCommand("qAAAAAAA", new Sony12(1, 21));

            // Previous
            receiver.AddIrCommand("PdIAAAAA", new Sony15(151, 60));

            // Replay
            receiver.AddIrCommand("n9IAAAAA", new Sony15(151, 121));

            // Advance
            receiver.AddIrCommand("HtIAAAAA", new Sony15(151, 120));

            // Next
            receiver.AddIrCommand("vNIAAAAA", new Sony15(151, 61));

            // Rewind
            receiver.AddIrCommand("2dIAAAAA", new Sony15(151, 27));

            // Play
            receiver.AddIrCommand("WNIAAAAA", new Sony15(151, 26));

            // FastForward
            receiver.AddIrCommand("ONIAAAAA", new Sony15(151, 28));

            // Sync Menu
            receiver.AddIrCommand("GrAAAAAA", new Sony15(26, 88));

            // Pause
            receiver.AddIrCommand("mNIAAAAA", new Sony15(151, 25));

            // Stop
            receiver.AddIrCommand("GNIAAAAA", new Sony15(151, 24));

            // Theater
            receiver.AddIrCommand("BtwAAAAA", new Sony15(119, 96));

            // Sound
            receiver.AddIrCommand("3tIAAAAA", new Sony15(151, 123));

            // Picture
            receiver.AddIrCommand("JgAAAAAA", new Sony12(1, 100));

            // Wide
            receiver.AddIrCommand("vEoAAAAA", new Sony15(164, 61));

            // DMeX
            receiver.AddIrCommand("1rAAAAAA", new Sony15(26, 107));

            // CC
            receiver.AddIrCommand("CEoAAAAA", new Sony15(164, 16));

            // Freeze
            receiver.AddIrCommand("OgAAAAAA", new Sony12(1, 92));

            // Guide
            receiver.AddIrCommand("cAAAAAAA", new Sony12(1, 14));

            // Favorites
            receiver.AddIrCommand("btwAAAAA", new Sony15(119, 118));

            // Input
            receiver.AddIrCommand("pAAAAAAA", new Sony12(1, 37));

            // Return
            receiver.AddIrCommand("xNIAAAAA", new Sony15(151, 35));

            // Home
            receiver.AddIrCommand("BgAAAAAA", new Sony12(1, 96));

            // Options
            receiver.AddIrCommand("bNIAAAAA", new Sony15(151, 54));

            // Numeric 1
            receiver.AddIrCommand("AQAAAAAA", new Sony12(1, 0));

            // Numeric 2
            receiver.AddIrCommand("gQAAAAAA", new Sony12(1, 1));

            // Numeric 3
            receiver.AddIrCommand("QQAAAAAA", new Sony12(1, 2));

            // Numeric 4
            receiver.AddIrCommand("wQAAAAAA", new Sony12(1, 3));

            // Numeric 5
            receiver.AddIrCommand("IQAAAAAA", new Sony12(1, 4));

            // Numeric 6
            receiver.AddIrCommand("oQAAAAAA", new Sony12(1, 5));

            // Numeric 7
            receiver.AddIrCommand("YQAAAAAA", new Sony12(1, 6));

            // Numeric 8
            receiver.AddIrCommand("4QAAAAAA", new Sony12(1, 7));

            // Numeric 9
            receiver.AddIrCommand("EQAAAAAA", new Sony12(1, 8));

            // Numeric 0
            receiver.AddIrCommand("kQAAAAAA", new Sony12(1, 9));

            // Period
            receiver.AddIrCommand("udIAAAAA", new Sony15(151, 29));

            // Enter
            receiver.AddIrCommand("0QAAAAAA", new Sony12(1, 11));

            // Channel +
            receiver.AddIrCommand("CQAAAAAA", new Sony12(1, 16));

            // Channel -
            receiver.AddIrCommand("iQAAAAAA", new Sony12(1, 17));

            // Vol +
            receiver.AddIrCommand("SQAAAAAA", new Sony12(1, 18));

            // Vol -
            receiver.AddIrCommand("yQAAAAAA", new Sony12(1, 19));

            // Mute
            receiver.AddIrCommand("KQAAAAAA", new Sony12(1, 20));

            // Jump
            receiver.AddIrCommand("3QAAAAAA", new Sony12(1, 59));

            // Select
            receiver.AddIrCommand("pwAAAAAA", new Sony12(1, 101));

            // Up
            receiver.AddIrCommand("LwAAAAAA", new Sony12(1, 49));

            // Left
            receiver.AddIrCommand("LQAAAAAA", new Sony12(1, 52));

            // Down
            receiver.AddIrCommand("rwAAAAAA", new Sony12(1, 50));

            // Right
            receiver.AddIrCommand("zQAAAAAA", new Sony12(1, 51));
        }
    }
}
