using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reactive;

namespace Storm
{
    public interface IThing
    {
        string InstanceId { get; set; }
    }
}
