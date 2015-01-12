using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.Payload.Audio
{
    public class SetVolume : IPayload
    {
        public double Volume { get; set; }
    }
}
