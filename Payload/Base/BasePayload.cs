using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.Payload
{
    public abstract class BasePayload : IPayload
    {
        public virtual string GetDebugInfo()
        {
            string displayTypeName = this.GetType().FullName;
            if (displayTypeName.StartsWith("Storm.Payload."))
                displayTypeName = displayTypeName.Substring(14);

            return displayTypeName;
        }
    }
}
