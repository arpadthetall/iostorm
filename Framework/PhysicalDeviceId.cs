using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using IoStorm.Addressing;

namespace IoStorm
{
    public static class PhysicalDeviceId
    {
        public static HubAddress GetHubAddress()
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
                return new HubAddress("GUID_" + Guid.NewGuid().ToString("n"));
            }

            return new HubAddress("MAC_" + nic.GetPhysicalAddress().ToString());
        }
    }
}
