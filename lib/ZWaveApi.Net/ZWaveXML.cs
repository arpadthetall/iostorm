using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace ZWaveApi.Net
{
    public class ZWaveXML
    {
        public static string language { get; private set; }

        private static XmlDocument xmlDoc = new XmlDocument();

        static ZWaveXML()
        {
            string filname = System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath + "bin\\ZWaveApi.xml";
            
            xmlDoc.Load(filname);
        }

        public static string code(string classType, string classKey)
        {
            XmlNode nodes = xmlDoc.SelectSingleNode("/ZWaveApi/" + classType + "s/" + classType + "[@key='0x" + classKey + "']");

            if (nodes.Attributes["code"].Value != "")
                return nodes.Attributes["code"].Value;
            else
                return classType + " : No code for this key 0x" + classKey;
        }

        public static string code(string classType, byte classKey)
        {
            return code(classType, classKey.ToString("X2"));
        }

        public static string code(string classType, int classKey)
        {
            return code(classType, classKey.ToString());
        }

        public static string text(string classType, string classKey, string language)
        {
            XmlNode nodes = xmlDoc.SelectSingleNode("/ZWaveApi/" + classType + "s/" + classType + "[@key='" + classKey + "']");
            string returnString = "";

            if (!(nodes == null))
            {
                foreach (XmlNode node in nodes)
                {
                    if (node.Attributes["key"].Value == language || (node.Attributes["key"].Value == "EN" && returnString == ""))
                        returnString = node.Attributes["text"].Value;
                }
            }

            if (returnString != "")
                return returnString;
            else
                return classType + " : No text for this key " + classKey;
        }

        public static string text(string classType, byte classKey, string language)
        {
            return text(classType, "0x"+classKey.ToString("X2"), language);
        }

        public static string text(string classType, byte classKey)
        {
            return text(classType, classKey, "EN");
        }

        public static string text(string classType, int classKey, string language)
        {
            return text(classType, classKey.ToString(), language);
        }

        public static string text(string classType, int classKey)
        {
            return text(classType, classKey.ToString(), "EN");
        }
    }
}
