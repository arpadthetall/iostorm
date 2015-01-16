using System;
using System.Collections.Generic;
using System.Threading;
using TidalWaveTest.Devices;

namespace TidalWaveTest
{
    class Program
    {
        private static List<byte> DeviceIds;

        static void Main()
        {
            DeviceIds = new List<byte>();

            var zp = new ZWavePort();
            zp.Open();


            zp.SubscribeToMessages(MessageHandler);
            zp.SendMessage(new byte[] {0x01, 0x03, 0x00, 0x02, 0xFE});


            //var d = new Dimmer(0x2B, zp);
            var s = new Switch(0x06, zp);

            //d.On();
            //d.Dim(50);
            //d.Off();

            s.On();
            Thread.Sleep(1000);
            s.Off();

            System.Console.ReadLine();

            zp.Close();
        }

        public static void MessageHandler(byte[] message)
        {
            if (message[0] == 0x06 || message[6] != 0x1D)
                return;

            // TODO: Some kind of BitConverter for this?

            byte offset = 0;
            for (var i = 7; i < message.Length; i++)
            {
                var b = message[i];

                for (byte bitNumber = 0; bitNumber < 8; bitNumber++)
                {
                    var bit = (b & (1 << bitNumber - 1)) != 0;
                    if (bit)
                    {
                        DeviceIds.Add((byte)(offset + bitNumber));
                    }
                }

                offset += 8;
            }
        }
    }
}
