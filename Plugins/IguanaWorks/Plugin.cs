//#define VERBOSE_IR_DATA

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Qlue.Logging;
using System.IO;
using IoStorm.Plugin;
using LibUsbDotNet;
using LibUsbDotNet.Info;
using LibUsbDotNet.Main;

namespace IoStorm.Plugins.IguanaWorks
{
    [Plugin(Name = "IguanaWorks IR", Description = "IguanaWorks IR transceiver", Author = "IoStorm")]
    public class Plugin : BaseDevice, IDisposable
    {
        private const byte STATE_MASK = 0x80;
        private const byte LENGTH_MASK = 0x7F;
        private const int IG_PULSE_BIT = 0x01000000;
        private const int IG_PULSE_MASK = 0x00FFFFFF;

        private ILog log;
        private IHub hub;
        private CancellationTokenSource cts;
        private UsbDevice usbDevice;
        private IUsbDevice wholeUsbDevice;
        private UsbEndpointReader reader;
        private UsbEndpointWriter writer;
        private bool isDeviceInitialized;
        private TimeSpan deviceInitializationTimeout;
        private byte waitForCommand;
        private ManualResetEvent waitForCommandEvent;
        private byte[] receivedPayload;
        private Task portMonitorTask;
        private IrData currentIrData;
        private List<DecoderBase> decoders;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("IguanaWorks");

            this.deviceInitializationTimeout = TimeSpan.FromSeconds(5);

            this.cts = new CancellationTokenSource();

            this.waitForCommandEvent = new ManualResetEvent(false);

            var receivedCommand = new Action<Payload.IIRProtocol>(cmd =>
            {
                var payload = new Payload.IRCommand
                {
                    Command = cmd
                };

                hub.BroadcastPayload(this, payload);
            });

            this.decoders = new List<DecoderBase>()
            {
                new DecoderNEC(this.log, receivedCommand),
                new DecoderNECx(this.log, receivedCommand),
                new DecoderHash(this.log, receivedCommand)
            };

            this.portMonitorTask = Task.Factory.StartNew(() => PortMonitor(), TaskCreationOptions.LongRunning);
        }

        private void PortMonitor()
        {
            this.log.Debug("PortMonitor running");

            while (!this.cts.IsCancellationRequested)
            {
                try
                {
                    this.isDeviceInitialized = false;

                    while (this.usbDevice == null)
                    {
                        try
                        {
                            this.reader = null;
                            this.writer = null;

                            var usbFinder = new UsbDeviceFinder(0x1781, 0x0938);
                            this.usbDevice = UsbDevice.OpenUsbDevice(usbFinder);

                            if (this.usbDevice == null)
                            {
                                this.log.Info("No IguanaWorks USB device found");

                                // Wait
                                this.cts.Token.WaitHandle.WaitOne(5000);

                                break;
                            }

                            // If this is a "whole" usb device (libusb-win32, linux libusb)
                            // it will have an IUsbDevice interface. If not (WinUSB) the 
                            // variable will be null indicating this is an interface of a 
                            // device.
                            this.wholeUsbDevice = this.usbDevice as IUsbDevice;
                            if (this.wholeUsbDevice != null)
                            {
                                // This is a "whole" USB device. Before it can be used, 
                                // the desired configuration and interface must be selected.

                                // Select config #1
                                this.wholeUsbDevice.SetConfiguration(1);

                                // Claim interface #0.
                                this.wholeUsbDevice.ClaimInterface(0);
                            }

                            this.log.Debug("Found IguanaWorks USB device");

                            var usbConfigs = this.usbDevice.Configs;
                            if (usbConfigs == null || usbConfigs.Count < 1)
                                throw new Exception("Invalid configuration on device");

                            var iface = usbConfigs[0].InterfaceInfoList[0];
                            if (iface.EndpointInfoList.Count < 2)
                                throw new Exception("Invalid list of endpoints");

                            UsbEndpointInfo usbEndpointInfoRead = iface.EndpointInfoList[0];
                            UsbEndpointInfo usbEndpointInfoWrite = iface.EndpointInfoList[1];

                            this.reader = this.usbDevice.OpenEndpointReader(
                              (ReadEndpointID)usbEndpointInfoRead.Descriptor.EndpointID,
                              0,
                              EndpointType.Interrupt);

                            this.writer = this.usbDevice.OpenEndpointWriter(
                              (WriteEndpointID)usbEndpointInfoWrite.Descriptor.EndpointID,
                              EndpointType.Interrupt);

                            this.log.Debug("USB Reader/Writer opened");
                        }
                        catch (Exception ex)
                        {
                            this.log.Debug("Unable to open USB port, error {0}", ex.Message);

                            this.cts.Token.WaitHandle.WaitOne(10000);
                            break;
                        }
                    }

                    if (this.usbDevice == null || reader == null || writer == null)
                        continue;

                    short readPacketSize = reader.EndpointInfo.Descriptor.MaxPacketSize;
                    short writePacketSize = writer.EndpointInfo.Descriptor.MaxPacketSize;
                    byte[] readBuf = new byte[readPacketSize];

                    Init();

                    DateTime portOpened = DateTime.Now;

                    while (!this.cts.IsCancellationRequested && this.usbDevice != null && this.usbDevice.IsOpen)
                    {
                        int readBytes;
                        var result = this.reader.Read(readBuf, 1000, out readBytes);

                        if (result == ErrorCode.Win32Error)
                        {
                            // Failed
                            this.log.Warn("IguanaWorks device failed");
                            break;
                        }
                        else if (result == ErrorCode.None)
                        {
                            if (readBytes > 0)
                            {
#if VERBOSE_IR_DATA
                                this.log.Trace("Received {0} bytes", readBytes);
#endif

                                if (this.waitForCommand != 0 && CheckReceivedData(readBuf, this.waitForCommand))
                                {
                                    if (readBytes > 4)
                                    {
                                        this.receivedPayload = new byte[readBytes - 4];
                                        Buffer.BlockCopy(readBuf, 4, this.receivedPayload, 0, readBytes - 4);
                                    }

                                    this.waitForCommand = 0;
                                    this.waitForCommandEvent.Set();
                                }
                                else
                                    AssembleIrData(readBuf, readBytes);
                            }
                        }

                        if (!this.isDeviceInitialized && (DateTime.Now - portOpened) > this.deviceInitializationTimeout)
                        {
                            this.log.Warn("Device is not initialized, attempt reset");

                            // Cycle the port available subscription
                            Close(false);

                            break;
                        }
                    }

                    if (this.usbDevice != null && this.usbDevice.IsOpen)
                        Close(false);
                }
                catch (Exception ex)
                {
                    this.log.WarnException(ex, "Exception in PortMonitor");

                    Close(false);
                }
            }

            this.log.Debug("PortMonitor closing");

            Close(true);
        }

        private void AssembleIrData(byte[] input, int bytesRead)
        {
            int length = bytesRead - 1;
            int[] output = new int[length];
            int inSpace = 0;
            int codeLength = 0;

            for (int x = 0; x < length + 1; x++)
            {
                if (x > 0 && (x == length ||
                     ((input[x] & STATE_MASK) != inSpace) ||
                     ((input[x] & LENGTH_MASK) + output[codeLength] > IG_PULSE_MASK)))
                {
                    output[codeLength] = (output[codeLength] << 6) / 3;

                    if (inSpace == 0)
                        output[codeLength] |= IG_PULSE_BIT;
                    codeLength++;

                    if (x == length)
                        break;
                    output[codeLength] = 0;
                }

                /* increase by the maximum pulse length + 1 */
                if ((input[x] & LENGTH_MASK) == 0)
                    output[codeLength] += 1023 + 1;
                else
                    output[codeLength] += (input[x] & LENGTH_MASK) + 1;

                inSpace = input[x] & STATE_MASK;
            }

            if (this.currentIrData == null)
            {
                this.currentIrData = new IrData
                {
                    FrequencyHertz = 0      // Unknown
                };
            }

            for (int x = 0; x < codeLength; x++)
            {
                int value = output[x];

                if ((value & IG_PULSE_BIT) != 0)
                {
#if VERBOSE_IR_DATA
                    this.log.Trace("Pulse {0}", (value & IG_PULSE_MASK));
#endif

                    this.currentIrData.AddData(value & IG_PULSE_MASK, true);
                }
                else
                {
#if VERBOSE_IR_DATA
                    this.log.Trace("Space {0}", value);
#endif

                    if (value > 5000)
                    {
                        // Gap between transmissions
                        if (!this.currentIrData.IsEmpty)
                        {
                            IrData irData = this.currentIrData;
                            // Decode
                            Task.Run(() => Decode(irData));

                            this.currentIrData = new IrData
                            {
                                FrequencyHertz = 0      // Unknown
                            };
                        }
                    }

                    this.currentIrData.AddData(value, false);
                }
            }
        }

        private void Decode(IrData irData)
        {
            this.log.Trace("Samples: {0}", irData.Data.Count);

            foreach (var decoder in this.decoders)
            {
#if VERBOSE_IR_DATA
                this.log.Trace("Attempt {0}", decoder.GetType().Name);
#endif
                if (decoder.Decode(irData))
                {
                    // Found
                    break;
                }
            }
        }

        private void Init()
        {
            Task.Factory.StartNew(() =>
            {
                lock (this)
                {
                    try
                    {
                        // Get firmware version
                        byte[] response = SendCommand(0x01);
                        if (response == null || response.Length != 2)
                            throw new Exception("Invalid version response");

                        this.log.Info("IguanaWorks firmware version 0x{0:x2}{1:x2}", response[0], response[1]);

                        // Get features
                        response = SendCommand(0x10);
                        if (response == null || response.Length != 2)
                            throw new Exception("Invalid feature response");

                        if ((response[0] & 0x01) != 0)
                            this.log.Debug("Has LEDS");
                        if ((response[0] & 0x02) != 0)
                            this.log.Debug("Has BOTH");
                        if ((response[0] & 0x04) != 0)
                            this.log.Debug("Has SOCKETS");
                        if ((response[0] & 0x08) != 0)
                            this.log.Debug("Has LCD");
                        if ((response[0] & 0x10) != 0)
                            this.log.Debug("Has SLOT_DEV");

                        // Turn receiver on
                        SendCommand(0x12);

                        this.log.Info("IguanaWorks initialized successfully");

                        this.isDeviceInitialized = true;
                    }
                    catch (Exception ex)
                    {
                        this.log.WarnException("Failed to initialize IguanaWorks", ex);
                    }
                }
            });
        }

        private byte[] SendCommand(byte command)
        {
            if (this.usbDevice == null)
                return null;

            byte[] msg = new byte[5];
            msg[0] = 0;
            msg[1] = 0;
            msg[2] = 0xCD;          // To device
            msg[3] = command;       // Code
            msg[4] = 0;             // Data Len

            this.waitForCommand = command;
            this.waitForCommandEvent.Reset();
            this.receivedPayload = null;

            int sentBytes;
            var result = this.writer.Write(msg, 0, 5, 1000, out sentBytes);

            if (sentBytes != 5)
                throw new Exception(string.Format("Failed to send all bytes (result {0})", result));

            if (!this.waitForCommandEvent.WaitOne(1000))
                throw new TimeoutException("Failed to receive response");

            return this.receivedPayload;
        }

        private bool CheckReceivedData(byte[] data, byte expectedCommand)
        {
            if (data == null)
                return false;

            if (data.Length <= 4)
                return false;

            if (expectedCommand == 0x12 && data[data.Length - 1] == 0x00)
                // The receiver may already be on
                return true;

            if (data[0] != 0x00)
                return false;

            if (data[1] != 0x00)
                return false;

            if (data[2] != 0xDC)
                return false;

            if (data[3] != expectedCommand)
                return false;

            return true;
        }

        private void Close(bool sendReceiverOff)
        {
            lock (this)
            {
                try
                {
                    if (this.usbDevice != null)
                    {
                        if (this.usbDevice.IsOpen)
                        {
                            if (sendReceiverOff)
                            {
                                // Send receiver off
                                byte[] msg = new byte[5];
                                msg[0] = 0;
                                msg[1] = 0;
                                msg[2] = 0xCD;          // To device
                                msg[3] = 0x14;          // Code
                                msg[4] = 0;             // Data Len

                                int sentBytes;
                                this.writer.Write(msg, 0, 5, 1000, out sentBytes);
                                // Ignore response
                            }

                            if (this.wholeUsbDevice != null)
                            {
                                // Release interface #0
                                this.wholeUsbDevice.ReleaseInterface(0);

                                this.wholeUsbDevice = null;
                            }

                            this.usbDevice.Close();
                        }

                        this.usbDevice = null;
                    }
                }
                catch (Exception ex)
                {
                    this.log.WarnException("Exception in Close", ex);
                }
            }
        }

        public void Dispose()
        {
            this.log.Trace("Disposing");

            this.cts.Cancel();

            this.portMonitorTask.Wait();

            this.log.Trace("Port Monitor stopped");

            UsbDevice.Exit();

            this.log.Trace("Disposing done");
        }

        public void Incoming(Payload.IRCommand payload)
        {
            this.log.Trace("Sending IR");
        }
    }
}
