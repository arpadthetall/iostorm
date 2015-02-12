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
        //public const string InstanceType_Zone = "ZONE";
        //public const string InstanceType_Plugin = "PLUG";
        //public const string InstanceType_Node = "NODE";
        //public const string InstanceType_PhysicalDeviceId = "PDI";
        //public const string InstanceType_App = "APP";

        public static T GetInstanceId<T>() where T : IoStorm.Addressing.InstanceAddress
        {
            return (T)Activator.CreateInstance(typeof(T), Guid.NewGuid().ToString("n"));
        }

        public static T GetZoneAddress<T>() where T : IoStorm.Addressing.ZoneAddress
        {
            return (T)Activator.CreateInstance(typeof(T), Guid.NewGuid().ToString("n"));
        }
    }
}
