using System;
using System.Collections.Generic;

namespace Storm.Payload.Light
{
    public class On : BasePayload
    {
        public string LightId { get; set; }
    }
}
