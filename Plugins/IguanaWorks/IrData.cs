using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IoStorm.Plugins.IguanaWorks
{
    public class IrData
    {
        private bool lastDataIsPulse;

        public int FrequencyHertz { get; set; }

        public List<int> Data { get; private set; }

        public void AddData(int lengthMicroSeconds, bool pulse)
        {
            if (!pulse && Data.Count == 1 && !this.lastDataIsPulse)
                return;

            if (Data.Count > 0 && pulse == this.lastDataIsPulse)
            {
                Data[Data.Count - 1] += lengthMicroSeconds;
            }
            else
                Data.Add(lengthMicroSeconds);

            this.lastDataIsPulse = pulse;
        }

        public IrData()
        {
            Data = new List<int>();
        }

        public bool IsEmpty
        {
            get
            {
                return Data.Count <= 1;
            }
        }
    }
}
