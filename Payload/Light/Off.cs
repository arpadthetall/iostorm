using System;
using System.Collections.Generic;

namespace Storm.Payload.Light
{
    public class Off : BasePayload
    {
        public string LightId { get; set; }
    }
}
