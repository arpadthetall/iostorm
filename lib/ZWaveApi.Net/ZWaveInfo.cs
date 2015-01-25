/* 
 *	Copyright (C) 2010- ZWaveApi
 *	http://ZWaveApi.net
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as published by
 *  the Free Software Foundation; either version 3, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU Lesser General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/lesser.html
 *
 */

using System.Runtime.Serialization;

namespace ZWaveApi.Net
{
    [DataContract]
    public class ZWaveInfo
    {
        private byte libraryType;
        private byte controllerCaps;

        #region DataMember
        
        [DataMember]
        public string Language { get; private set; }

        [DataMember]
        public string LibraryVersion { get; private set; }

        [DataMember]
        public string LibraryType
        {
            private set { LibraryType = ""; }
            get
            {
                string libraryTypeName;

                if (this.libraryType < 9) 
                    libraryTypeName = ZWaveXML.text("LibraryTypeName", this.libraryType.ToString(), this.Language);
                else
                    libraryTypeName = ZWaveXML.text("LibraryTypeName", 0, this.Language);

                return libraryTypeName;
            }
        }

        [DataMember]
        public string ControllerCaps
        {
            private set { ControllerCaps = ""; }
            get
            {
                string controllerCapsName = "";

                if ((this.controllerCaps & 0x04) != 0x00) // There is a SUC ID Server on the network
                {
                    controllerCapsName = "    - There is a SUC ID Server (SIS) in this network.\n" +
                                                "    - The PC controller is an inclusion controller.\n";

                    if ((this.controllerCaps & 0x08) != 0x00)		// Controller was the primary before the SIS was added.
                        controllerCapsName = controllerCapsName + "    - It was the primary before the SIS was added.\n";
                    else
                        controllerCapsName = controllerCapsName + "    - It was a secondary before the SIS was added.\n";

                }
                else
                {
                    controllerCapsName = "    - There is no SUC ID Server in the network.\n";

                    if ((this.controllerCaps & 0x01) != 0x00)		// The controller is a secondary.
                        controllerCapsName = controllerCapsName + "    - The PC controller is a secondary controller.\n";
                    else
                        controllerCapsName = controllerCapsName + "    - The PC controller is a primary controller.\n";

                }

                if ((this.controllerCaps & 0x10) != 0x00)		// Controller is a static update controller.
                    controllerCapsName = controllerCapsName + "    - The PC controller is also a Static Update Controller.\n";

                return controllerCapsName;
            }
        }

        [DataMember]
        public uint homeId { get; private set; }

        [DataMember]
        public byte controlNodeID { get; private set; }

        [DataMember]
        public byte ApplicationVersion { get; private set; }

        [DataMember]
        public byte ApplicationRevision { get; private set; }

        [DataMember]
        public uint ManufacturerId { get; private set; }

        [DataMember]
        public uint ProductType { get; private set; }

        [DataMember]
        public uint ProductId { get; private set; }

        #endregion

        public ZWaveInfo()
        {
            this.Language = "EN";
        }

        public ZWaveInfo(string language)
        {
            this.Language = language;
        }

        public void getVersion(byte[] buffer)
        {
            string s = System.Text.ASCIIEncoding.ASCII.GetString(buffer);

            this.LibraryVersion = s.Substring(4, s.IndexOf((char)0x00) - 4);
            this.libraryType = buffer[(byte)(this.LibraryVersion.Length + 5)];
        }

        public void MemoryGetId(byte[] buffer)
        {
            this.homeId = (((uint)buffer[4]) << 24) | (((uint)buffer[5]) << 16) | (((uint)buffer[6]) << 8) | ((uint)buffer[7]);
            this.controlNodeID = buffer[8];
        }

        public void GetControllerCapabilities(byte[] buffer)
        {
            this.controllerCaps = buffer[4];
        }

        public void SerialGetCapabilities(byte[] buffer)
        {
            this.ApplicationVersion = buffer[4];
            this.ApplicationRevision = buffer[5];
            this.ManufacturerId = (((uint)buffer[6]) << 8) | ((uint)buffer[7]);
            this.ProductType = (((uint)buffer[8]) << 8) | ((uint)buffer[9]);
            this.ProductId = (((uint)buffer[10]) << 8) | ((uint)buffer[11]);
        }
    }
}
