using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LircSvc
{
    public static class LircCommands
    {
        public static class Navigation
        {
            public const string Up = "KEY_UP";
            public const string Down = "KEY_DOWN";
            public const string Left = "KEY_LEFT";
            public const string Right = "KEY_RIGHT";
            public const string Num1 = "KEY_1";
            public const string Num2 = "KEY_2";
            public const string Num3 = "KEY_3";
            public const string Num4 = "KEY_4";
            public const string Num5 = "KEY_5";
            public const string Num6 = "KEY_6";
            public const string Num7 = "KEY_7";
            public const string Num8 = "KEY_8";
            public const string Num9 = "KEY_9";
            public const string Num0 = "KEY_0";
        }

        public static class Power
        {
            public const string Toggle = "KEY_POWER";
        }

        public static class Transport
        {
            public const string Advance = "KEY_FORWARD";
            public const string FastForward = "KEY_FASTFORWARD";
            public const string Next = "KEY_NEXT";
            public const string Pause = "KEY_PAUSE";
            public const string Play = "KEY_PLAY";
            public const string Previous = "KEY_PREVIOUS";
            public const string Rewind = "KEY_REWIND";
            public const string Stop = "KEY_STOP";

            public const string Shuffle = "UNUSED_KEY_SHUFFLE";
            public const string Repeat = "UNUSED_KEY_REPEAT";
            public const string Replay = "UNUSED_KEY_REPLAY";
        }
    }
}
