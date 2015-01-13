using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class ChangeVolume : IPayload
    {
        public double Steps { get; set; }
    }
}
