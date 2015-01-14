using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Audio
{
    public class ChangeVolume : BasePayload
    {
        public double Steps { get; set; }
    }
}
