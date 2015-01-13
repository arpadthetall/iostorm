using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class SetVolume : IPayload
    {
        public double Volume { get; set; }
    }
}
