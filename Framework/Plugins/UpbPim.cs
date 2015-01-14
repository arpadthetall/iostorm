using System;
using System.Collections.Generic;
using System.Text;
using Qlue.Logging;

namespace Storm.Plugins
{
    public class UpbPim : BaseDevice, IDisposable
    {
        public class UpbPimMessage : Payload.UpbCommand.UpbMessage
        {
            public UpbPimMessage()
            {
                DestinationType = DestinationTypes.Device;
                SourceId = 0xFF;
                SendX = SendRepeats.One;
                SendTime = SendRepeats.One;
                AckPulse = true;
                IdPulse = false;
                AckMessage = false;
                Repeater = PowerlineRepeaters.None;
            }

            public byte[] GetGeneratedBytes()
            {
                if (SendTime > SendX)
                    throw new ArgumentException("Can't be greater than SendX");

                int? level = null;
                int? rate = null;
                int? blinkRate = null;
                int? toggleCount = null;
                int? toggleRate = null;
                int? channel = null;

                byte commandValue;
                switch (Command)
                {
                    case UpbCommands.Activate:
                        commandValue = 0x20;
                        break;

                    case UpbCommands.Deactivate:
                        commandValue = 0x21;
                        break;

                    case UpbCommands.Goto:
                        if (!Level.HasValue)
                            throw new ArgumentNullException("Level missing");
                        commandValue = 0x22;
                        level = Level.Value;
                        rate = Rate;
                        if (Rate.HasValue && DestinationType == DestinationTypes.Device && Channel.HasValue)
                            channel = Channel.Value;
                        break;

                    case UpbCommands.FadeStart:
                        if (!Level.HasValue)
                            throw new ArgumentNullException("Level missing");
                        commandValue = 0x23;
                        level = Level.Value;
                        rate = Rate;
                        if (Rate.HasValue && DestinationType == DestinationTypes.Device && Channel.HasValue)
                            channel = Channel.Value;
                        break;

                    case UpbCommands.FadeStop:
                        commandValue = 0x24;
                        level = Level;
                        break;

                    case UpbCommands.Blink:
                        if (!BlinkRate.HasValue)
                            throw new ArgumentNullException("BlinkRate missing");
                        commandValue = 0x25;
                        blinkRate = BlinkRate.Value;
                        if (Rate.HasValue && DestinationType == DestinationTypes.Device && Channel.HasValue)
                            channel = Channel.Value;
                        break;

                    case UpbCommands.Indicate:
                        commandValue = 0x26;
                        level = Level;
                        rate = Rate;
                        if (Rate.HasValue && DestinationType == DestinationTypes.Device && Channel.HasValue)
                            channel = Channel.Value;
                        break;

                    case UpbCommands.Toggle:
                        if (!ToggleCount.HasValue)
                            throw new ArgumentNullException("ToggleCount missing");
                        commandValue = 0x27;
                        level = Level;
                        toggleCount = ToggleCount.Value;
                        toggleRate = ToggleRate;
                        if (Rate.HasValue && DestinationType == DestinationTypes.Device && Channel.HasValue)
                            channel = Channel.Value;
                        break;

                    case UpbCommands.ReportState:
                        commandValue = 0x30;
                        break;

                    case UpbCommands.StoreState:
                        commandValue = 0x31;
                        break;

                    case UpbCommands.AckResponse:
                        commandValue = 0x80;
                        break;

                    case UpbCommands.SetupTimeReport:
                        commandValue = 0x85;
                        break;

                    case UpbCommands.DeviceStateReport:
                        if (!Level.HasValue)
                            throw new ArgumentNullException("Level missing");
                        commandValue = 0x86;
                        break;

                    case UpbCommands.DeviceStatusReport:
                        commandValue = 0x87;
                        break;

                    case UpbCommands.DeviceSignatureReport:
                        commandValue = 0x8f;
                        break;

                    case UpbCommands.RegisterValuesReport:
                        commandValue = 0x90;
                        break;

                    case UpbCommands.RAMvaluesReport:
                        commandValue = 0x91;
                        break;

                    case UpbCommands.RawDataReport:
                        commandValue = 0x92;
                        break;

                    case UpbCommands.HeartbeatReport:
                        commandValue = 0x93;
                        break;

                    default:
                        // Unknown/Invalid command
                        throw new ArgumentException("Unknown/Invalid command");
                }

                byte nibble1 = 0;
                if (DestinationType == DestinationTypes.Link)
                    nibble1 += 8;

                switch (Repeater)
                {
                    case PowerlineRepeaters.One:
                        nibble1 += 2;
                        break;

                    case PowerlineRepeaters.Two:
                        nibble1 += 4;
                        break;

                    case PowerlineRepeaters.Four:
                        nibble1 += 6;
                        break;
                }

                byte nibble3 = 0;

                if (AckPulse)
                    nibble3 += 1;

                if (IdPulse)
                    nibble3 += 2;

                if (AckMessage)
                    nibble3 += 4;

                byte nibble4 = 0;

                switch (SendX)
                {
                    case SendRepeats.Two:
                        nibble4 += 4;
                        break;

                    case SendRepeats.Three:
                        nibble4 += 8;
                        break;

                    case SendRepeats.Four:
                        nibble4 += 12;
                        break;
                }

                switch (SendTime)
                {
                    case SendRepeats.Two:
                        nibble4 += 1;
                        break;

                    case SendRepeats.Three:
                        nibble4 += 2;
                        break;

                    case SendRepeats.Four:
                        nibble4 += 3;
                        break;
                }

                var additional = new List<byte>();
                if (level.HasValue)
                    additional.Add((byte)level.Value);
                if (rate.HasValue)
                    additional.Add((byte)rate.Value);
                if (blinkRate.HasValue)
                    additional.Add((byte)blinkRate.Value);
                if (toggleCount.HasValue)
                    additional.Add((byte)toggleCount.Value);
                if (toggleRate.HasValue)
                    additional.Add((byte)toggleRate.Value);
                if (channel.HasValue)
                    additional.Add((byte)channel.Value);

                byte packetLength = (byte)(7 + additional.Count);

                byte[] result = new byte[packetLength];

                result[0] = (byte)(nibble1 << 4 | packetLength);
                result[1] = (byte)(nibble3 << 4 | nibble4);
                result[2] = NetworkId;
                result[3] = Id;
                result[4] = SourceId;
                result[5] = commandValue;
                for (int i = 0; i < additional.Count; i++)
                    result[6 + i] = additional[i];

                result[result.Length - 1] = CalculateChecksum(result, 0, result.Length - 1);

                return result;
            }

            public static byte CalculateChecksum(byte[] bytes, int offset, int length)
            {
                byte checksum = 0;
                for (int i = 0; i < length; i++)
                    checksum += bytes[offset + i];

                checksum = (byte)(256 - checksum);

                return checksum;
            }

            public static UpbPimMessage Decode(string input)
            {
                if (input.Length % 2 != 0)
                    // Invalid length
                    return null;

                byte[] bytes = new byte[input.Length / 2];

                for (int i = 0, j = 0; i < input.Length; i += 2)
                {
                    bytes[j++] = byte.Parse(input.Substring(i, 2), System.Globalization.NumberStyles.HexNumber);
                }

                if (bytes.Length < 7)
                    // Invalid length
                    return null;

                // Verify checksum
                byte checksum = CalculateChecksum(bytes, 0, bytes.Length - 1);
                if (bytes[bytes.Length - 1] != checksum)
                    // Invalid checksum
                    return null;

                var result = new UpbPimMessage();

                byte nibble1 = (byte)(bytes[0] >> 4);
                byte packetLength = (byte)(bytes[0] & 0x0F);
                byte nibble3 = (byte)(bytes[1] >> 4);
                byte nibble4 = (byte)(bytes[1] & 0x0F);

                result.DestinationType = (nibble1 & 0x08) != 0 ? DestinationTypes.Link : DestinationTypes.Device;
                switch (nibble1)
                {
                    case 0:
                        result.Repeater = PowerlineRepeaters.None;
                        break;

                    case 2:
                        result.Repeater = PowerlineRepeaters.One;
                        break;

                    case 4:
                        result.Repeater = PowerlineRepeaters.Two;
                        break;

                    case 6:
                        result.Repeater = PowerlineRepeaters.Four;
                        break;
                }

                if (packetLength != bytes.Length)
                    // Invalid length
                    return null;

                result.AckPulse = (nibble3 & 0x01) != 0;
                result.IdPulse = (nibble3 & 0x02) != 0;
                result.AckMessage = (nibble3 & 0x04) != 0;

                switch (nibble4 & 0x03)
                {
                    case 0:
                        result.SendTime = SendRepeats.One;
                        break;

                    case 1:
                        result.SendTime = SendRepeats.Two;
                        break;

                    case 2:
                        result.SendTime = SendRepeats.Three;
                        break;

                    case 3:
                        result.SendTime = SendRepeats.Four;
                        break;
                }

                switch (nibble4 & 0x0C)
                {
                    case 0:
                        result.SendX = SendRepeats.One;
                        break;

                    case 4:
                        result.SendX = SendRepeats.Two;
                        break;

                    case 8:
                        result.SendX = SendRepeats.Three;
                        break;

                    case 12:
                        result.SendX = SendRepeats.Four;
                        break;
                }

                if (result.SendTime > result.SendX)
                    // SendTime can't be greater than SendX
                    return null;

                result.NetworkId = bytes[2];
                result.Id = bytes[3];
                result.SourceId = bytes[4];
                switch (bytes[5])
                {
                    case 0x20:
                        result.Command = UpbCommands.Activate;
                        break;

                    case 0x21:
                        result.Command = UpbCommands.Deactivate;
                        break;

                    case 0x22:
                        result.Command = UpbCommands.Goto;
                        switch(packetLength)
                        {
                            case 7:
                                // Missing level
                                return null;

                            case 8:
                                result.Level = bytes[6];
                                break;

                            case 9:
                                result.Rate = bytes[7];
                                break;
                        }
                        break;

                    case 0x23:
                        result.Command = UpbCommands.FadeStart;
                        if (packetLength > 7)
                            result.Level = bytes[6];
                        else
                            // Missing level
                            return null;

                        if (packetLength > 8)
                            result.Rate = bytes[7];
                        break;

                    case 0x24:
                        result.Command = UpbCommands.FadeStop;
                        if (packetLength > 7)
                            result.Level = bytes[6];
                        break;

                    case 0x25:
                        result.Command = UpbCommands.Blink;
                        if (packetLength > 7)
                            result.BlinkRate = bytes[6];
                        else
                            // Missing BlinkRate
                            return null;
                        break;

                    case 0x26:
                        result.Command = UpbCommands.Indicate;
                        if (packetLength > 7)
                            result.Level = bytes[6];

                        if (packetLength > 8)
                            result.Rate = bytes[7];
                        break;

                    case 0x27:
                        result.Command = UpbCommands.Toggle;
                        switch(packetLength)
                        {
                            case 7:
                                // Missing level
                                return null;

                            case 8:
                                result.Level = bytes[6];
                                break;

                            case 9:
                                result.ToggleCount = bytes[7];
                                break;

                            case 10:
                                result.ToggleRate = bytes[7];
                                break;
                        }
                        if (packetLength > 7)
                            result.Level = bytes[6];
                        else
                            // Missing ToggleCount?
                            return null;
                        break;

                    case 0x30:
                        result.Command = UpbCommands.ReportState;
                        break;

                    case 0x31:
                        result.Command = UpbCommands.StoreState;
                        break;

                    case 0x80:
                        result.Command = UpbCommands.AckResponse;
                        break;

                    case 0x85:
                        result.Command = UpbCommands.SetupTimeReport;
                        break;

                    case 0x86:
                        result.Command = UpbCommands.DeviceStateReport;
                        if (packetLength > 7)
                            result.Level = bytes[6];
                        break;

                    case 0x87:
                        result.Command = UpbCommands.DeviceStatusReport;
                        break;

                    case 0x8F:
                        result.Command = UpbCommands.DeviceSignatureReport;
                        break;

                    case 0x90:
                        result.Command = UpbCommands.RegisterValuesReport;
                        break;

                    case 0x91:
                        result.Command = UpbCommands.RAMvaluesReport;
                        break;

                    case 0x92:
                        result.Command = UpbCommands.RawDataReport;
                        break;

                    case 0x93:
                        result.Command = UpbCommands.HeartbeatReport;
                        break;

                    default:
                        // Invalid command
                        return null;
                }

                return result;
            }
        }

        public enum SendTypes
        {
            Transmit = 0x14,
            ReadRegister = 0x12,
            WriteRegister = 0x17
        }

        protected class RawSendCommand
        {
            public SendTypes CommandType { get; private set; }

            public byte[] Bytes { get; private set; }

            public RawSendCommand(SendTypes commandType, params byte[] bytes)
            {
                CommandType = commandType;
                Bytes = bytes;
            }
        }

        private Qlue.Logging.ILog log;
        private IHub hub;
        private SerialLineManager serialManager;
        private Tuple<RawSendCommand, string>[] initData;
        private int initState;

        public UpbPim(ILogFactory logFactory, IHub hub, string serialPortName)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("UPB");

            this.serialManager = new SerialLineManager(logFactory, serialPortName, 4800);

            this.initData = new Tuple<RawSendCommand, string>[]
            {
                Tuple.Create(new RawSendCommand(SendTypes.ReadRegister, 0x0A, 0x02), "PR0A*"),
                Tuple.Create(new RawSendCommand(SendTypes.WriteRegister, 0x70, 0xFF), "PA")
            };

            this.serialManager.PortAvailable.Subscribe(isOpen =>
            {
                if (isOpen)
                {
                    Reset();
                }
            });

            this.serialManager.LineReceived
                .Subscribe(data =>
                {
                    this.log.Trace("Recevied {0}", data);

                    if (!this.serialManager.IsDeviceInitialized)
                    {
                        if (this.initState < 0 || this.initState >= this.initData.Length)
                            return;

                        string compareData = this.initData[this.initState].Item2;
                        bool success = false;
                        if (compareData.EndsWith("*"))
                        {
                            // Wildcard data
                            if (data.StartsWith(compareData.Substring(0, compareData.Length - 1)))
                            {
                                string wildcardData = data.Substring(compareData.Length - 1);

                                this.log.Debug("Wildcard data: {0}", wildcardData);

                                success = true;
                            }
                        }
                        else if (data == compareData)
                            success = true;

                        if (success)
                        {
                            this.initState++;
                            if (this.initState >= this.initData.Length)
                            {
                                // Done
                                this.serialManager.IsDeviceInitialized = true;
                            }
                            else
                            {
                                SendUPB(this.initData[this.initState].Item1);
                            }
                        }

                        return;
                    }

                    // Other data when running
                    if (data.StartsWith("PU"))
                    {
                        // Decode UPB
                        var upbMessage = UpbPimMessage.Decode(data.Substring(2));

                        if (upbMessage != null)
                        {
                            this.hub.BroadcastPayload(this, new Payload.UpbCommand
                                {
                                    Message = upbMessage
                                });
                        }
                    }

                });

            this.serialManager.Start();
        }

        private void Reset()
        {
            this.serialManager.DtrEnable = true;

            if (this.initData.Length > 0)
            {
                this.serialManager.IsDeviceInitialized = false;

                this.initState = 0;
                SendUPB(this.initData[this.initState].Item1);
            }
            else
                this.serialManager.IsDeviceInitialized = true;
        }

        private void SendUPB(UpbPimMessage upbMessage)
        {
            SendUPB(SendTypes.Transmit, upbMessage.GetGeneratedBytes());
        }

        private void SendUPB(RawSendCommand sendCommand)
        {
            SendUPB(sendCommand.CommandType, sendCommand.Bytes);
        }

        private void SendUPB(SendTypes commandType, params byte[] command)
        {
            byte checksum = UpbPimMessage.CalculateChecksum(command, 0, command.Length);

            var output = new StringBuilder();
            foreach (byte b in command)
                output.Append(b.ToString("X2"));

            this.log.Trace("Send {0} data {1}", commandType, output);

            this.serialManager.Write(string.Format("{0}{1}{2:X2}\r", Convert.ToChar((byte)commandType), output, checksum));
        }

        public void Incoming(Payload.Light.On payload)
        {
            if (!this.serialManager.IsDeviceInitialized)
                return;

            var cmd = new UpbPimMessage
            {
                Id = byte.Parse(payload.LightId),
                NetworkId = 166,
                DestinationType = Payload.UpbCommand.UpbMessage.DestinationTypes.Device,
                Command = Payload.UpbCommand.UpbMessage.UpbCommands.Goto,
                Level = 100
            };

            SendUPB(cmd);
        }

        public void Incoming(Payload.Light.Off payload)
        {
            if (!this.serialManager.IsDeviceInitialized)
                return;

            var cmd = new UpbPimMessage
            {
                Id = byte.Parse(payload.LightId),
                NetworkId = 166,
                DestinationType = Payload.UpbCommand.UpbMessage.DestinationTypes.Device,
                Command = Payload.UpbCommand.UpbMessage.UpbCommands.Goto,
                Level = 0
            };

            SendUPB(cmd);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.serialManager.Dispose();
            }
        }
    }
}
