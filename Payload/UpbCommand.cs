using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.Payload
{
    public class UpbCommand : BasePayload
    {
        public class UpbMessage
        {
            public enum UpbCommands
            {
                Activate,
                Deactivate,
                Goto,
                FadeStart,
                FadeStop,
                Blink,
                Indicate,
                Toggle,
                ReportState,
                StoreState,
                AckResponse,
                SetupTimeReport,
                DeviceStateReport,
                DeviceStatusReport,
                DeviceSignatureReport,
                RegisterValuesReport,
                RAMvaluesReport,
                RawDataReport,
                HeartbeatReport
            }

            public enum DestinationTypes
            {
                Device,
                Link
            }

            public enum PowerlineRepeaters
            {
                None,
                One,
                Two,
                Four
            }

            public enum SendRepeats
            {
                One,
                Two,
                Three,
                Four
            }

            public byte Id { get; set; }

            public byte NetworkId { get; set; }

            public DestinationTypes DestinationType { get; set; }

            public byte SourceId { get; set; }

            public UpbCommands Command { get; set; }

            public int? Level { get; set; }

            public int? Rate { get; set; }

            public int? Channel { get; set; }

            public SendRepeats SendX { get; set; }

            public SendRepeats SendTime { get; set; }

            public bool AckPulse { get; set; }

            public bool IdPulse { get; set; }

            public bool AckMessage { get; set; }

            public PowerlineRepeaters Repeater { get; set; }

            public int? BlinkRate { get; set; }

            public int? ToggleCount { get; set; }

            public int ToggleRate { get; set; }
        }

        public UpbMessage Message { get; set; }

        public override string GetDebugInfo()
        {
            return string.Format("UPB Cmd {0}", Message.Command);
        }
    }
}
