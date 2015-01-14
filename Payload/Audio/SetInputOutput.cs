using System;
using System.Collections.Generic;

namespace Storm.Payload.Audio
{
    public class SetInputOutput : BasePayload
    {
        public int Input { get; set; }

        public int Output { get; set; }
    }
}
