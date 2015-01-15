using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Audio
{
    public class SetVolume : BasePayload
    {
        public double Volume { get; set; }
    }
}
