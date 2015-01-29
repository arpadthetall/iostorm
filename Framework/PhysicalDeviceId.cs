using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm
{
    public static class PhysicalDeviceId
    {
        public static string GetDeviceId()
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up &&
                    x.NetworkInterfaceType == NetworkInterfaceType.Ethernet).FirstOrDefault();

            if (nic == null)
            {
                // Check Wireless
                nic = NetworkInterface.GetAllNetworkInterfaces()
                .Where(x => x.OperationalStatus == OperationalStatus.Up &&
                    x.NetworkInterfaceType == NetworkInterfaceType.Wireless80211).FirstOrDefault();
            }

            if (nic == null)
            {
                // None?
                return InstanceId.InstanceType_PhysicalDeviceId + ":" + Guid.NewGuid().ToString("n");
            }

            return InstanceId.InstanceType_PhysicalDeviceId + ":" + nic.GetPhysicalAddress().ToString();
        }
    }
}
