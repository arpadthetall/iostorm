using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.Payload
{
    public class IRCommand : BasePayload
    {
        public IIRProtocol Command { get; set; }

        public override string GetDebugInfo()
        {
            return string.Format("IRCommand {0} [{1}]", Command.GetType().Name, Command.ToString());
        }
    }
}
