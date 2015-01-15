using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Payload
{
    public class IRCommand : BasePayload
    {
        public IIRProtocol Command { get; set; }

        public int Repeat { get; set; }

        public override string GetDebugInfo()
        {
            return string.Format("IRCommand {0} [{1}]/{2}", Command.GetType().Name, Command.ToString(), Repeat);
        }
    }
}
