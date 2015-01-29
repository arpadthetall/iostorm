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
        public const string InstanceType_Zone = "ZONE";
        public const string InstanceType_Plugin = "PLUG";
        public const string InstanceType_Node = "NODE";

        public static string GetInstanceId(string type)
        {
            return type + ":" + Guid.NewGuid().ToString("n");
        }
    }
}
