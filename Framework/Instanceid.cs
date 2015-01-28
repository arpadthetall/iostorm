using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public static class InstanceId
    {
        public static string GetInstanceId()
        {
            return Guid.NewGuid().ToString("n");
        }
    }
}
