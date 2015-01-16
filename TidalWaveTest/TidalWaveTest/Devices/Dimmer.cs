namespace TidalWaveTest.Devices
{
    class Dimmer : Switch
    {
        public Dimmer(byte nodeId, ZWavePort zp)
            : base(nodeId, zp)
        {
        }

        public void Dim(byte level)
        {
            if (level > 99)
                level = 99;

            SetLevel(level);
        }
    }
}
