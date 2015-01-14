using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Audio
{
    public class VolumeDown : ChangeVolume
    {
        public VolumeDown()
        {
            Steps = -1;
        }
    }
}
