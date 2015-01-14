using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Audio
{
    public class SetInputOutput : BasePayload
    {
        public int Input { get; set; }

        public int Output { get; set; }
    }
}
