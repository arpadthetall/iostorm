using System;
using System.Collections.Generic;

namespace IoStorm.Payload.Light
{
    public class On : BasePayload
    {
        public string LightId { get; set; }
    }
}
