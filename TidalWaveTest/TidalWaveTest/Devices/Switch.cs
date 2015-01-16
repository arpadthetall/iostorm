using System.Threading;

namespace TidalWaveTest.Devices
{
    class Switch : ZWaveNode
    {
        public Switch(byte nodeId, ZWavePort zp)
            : base(nodeId, zp)
        {
        }

        public void On()
        {
            const byte level = 0xFF; // On
            SetLevel(level);
        }

        public void Off()
        {
            const byte level = 0x00; // Off
            SetLevel(level);
        }

        protected void SetLevel(byte level)
        {
            var message = new byte[] { 0x01, 0x0A, 0x00, 0x13, NodeId, 0x03, 0x20, 0x01, level, 0x05, 0x00, 0x00 };
            while (!SendMessage(message)) Thread.Sleep(100);
        }
    }
}
