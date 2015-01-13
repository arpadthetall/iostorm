using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class VolumeDown : ChangeVolume
    {
        public VolumeDown()
        {
            Steps = -1;
        }
    }
}
