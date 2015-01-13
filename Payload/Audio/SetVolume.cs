using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class SetVolume : BasePayload
    {
        public double Volume { get; set; }
    }
}
