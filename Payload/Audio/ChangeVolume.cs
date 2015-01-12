using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.Payload.Audio
{
    public class ChangeVolume : IPayload
    {
        public double Steps { get; set; }
    }
}
