using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class ChangeVolume : BasePayload
    {
        public double Steps { get; set; }
    }
}
