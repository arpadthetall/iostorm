using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class VolumeUp : ChangeVolume
    {
        public VolumeUp()
        {
            Steps = 1;
        }
    }
}
