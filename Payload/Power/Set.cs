using System;
using System.Collections.Generic;

namespace Storm.Payload.Power
{
    public class Set : IPayload
    {
        public bool Value { get; set; }
    }
}
