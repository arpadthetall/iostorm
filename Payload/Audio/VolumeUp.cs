using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Audio
{
    public class VolumeUp : ChangeVolume
    {
        public VolumeUp()
        {
            Steps = 1;
        }
    }
}
