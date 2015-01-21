using System;
using System.Collections.Generic;

namespace IoStorm.IRProtocol
{
    public class Hash : IoStorm.Payload.IIRProtocol
    {
        public Hash(string hashCode)
        {
            Code = hashCode;
        }

        public string Code { get; private set; }

        public override string ToString()
        {
            return string.Format("Code {0}", Code);
        }

        public int CompareTo(object obj)
        {
            var cmp = (Hash)obj;
            return cmp.Code.CompareTo(Code);
        }
    }
}
