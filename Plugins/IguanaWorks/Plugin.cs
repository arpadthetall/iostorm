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
        private ILog log;
        private IHub hub;
        private CancellationTokenSource cts;
        private UsbDevice usbDevice;
        private IUsbDevice wholeUsbDevice;
        private UsbEndpointReader reader;
        private UsbEndpointWriter writer;
        private ISubject<byte[]> receivedData;
        private bool portOpened;
        private bool isDeviceInitialized;
        private TimeSpan deviceInitializationTimeout;

        public Plugin(Qlue.Logging.ILogFactory logFactory, IHub hub, string instanceId)
            : base(instanceId)
        {
            this.hub = hub;

            this.log = logFactory.GetLogger("IguanaWorks");

            this.deviceInitializationTimeout = TimeSpan.FromSeconds(5);

            this.cts = new CancellationTokenSource();

            this.receivedData = new Subject<byte[]>();

            Task.Factory.StartNew(() => PortMonitor(), TaskCreationOptions.LongRunning);
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

                    this.portOpened = true;
                    Init();

                    DateTime portOpened = DateTime.Now;

                    while (!this.cts.IsCancellationRequested && this.usbDevice != null && this.usbDevice.IsOpen)
                    {
                        int readBytes;
                        var result = this.reader.Read(readBuf, 1000, out readBytes);

                        if (result == ErrorCode.Win32Error)
                            // Failed
                            break;
                        else if (result == ErrorCode.None)
                        {
                            if (readBytes > 0)
                            {
                                this.log.Trace("Received {0} bytes", readBytes);

                                // We have data
                                byte[] notifyBytes = new byte[readBytes];

                                // Copy buffer
                                Buffer.BlockCopy(readBuf, 0, notifyBytes, 0, readBytes);

                                this.receivedData.OnNext(notifyBytes);
                            }
                        }

                        if (!this.isDeviceInitialized && (DateTime.Now - portOpened) > this.deviceInitializationTimeout)
                        {
                            this.log.Warn("Device is not initialized, attempt reset");

                            // Cycle the port available subscription
                            Close();

                            break;
                        }
                    }

                    if (this.usbDevice != null && this.usbDevice.IsOpen)
                        Close();
                }
                catch (Exception ex)
                {
                    this.log.WarnException(ex, "Exception in PortMonitor");

                    Close();
                }
            }

            this.log.Debug("PortMonitor closing");
        }

        private void Init()
        {
            Task.Factory.StartNew(async () =>
            {
                try
                {
                    // Get firmware version
                    byte[] response = await SendCommand(0x01);
                    if (response == null || response.Length != 2)
                        throw new Exception("Invalid version response");

                    this.log.Info("IguanaWorks firmware version 0x{0:x2}{1:x2}", response[0], response[1]);

                    // Get features
                    response = await SendCommand(0x10);
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
                    await SendCommand(0x12);

                    this.log.Info("IguanaWorks initialized successfully");

                    this.isDeviceInitialized = true;
                }
                catch (Exception ex)
                {
                    this.log.WarnException("Failed to initialize IguanaWorks", ex);
                }
            });
        }

        private async Task<byte[]> SendCommand(byte command)
        {
            if (this.usbDevice == null)
                return null;

            byte[] msg = new byte[8];
            msg[0] = 0;
            msg[1] = 0;
            msg[2] = 0xCD;          // To device
            msg[3] = command;       // Code
            msg[4] = 0;             // Data Len

            var firstResponse = this.receivedData
                .Where(x => CheckReceivedData(x, command))
                .Buffer(TimeSpan.FromSeconds(1), 1)
                .FirstAsync();

            int sentBytes;
            var result = this.writer.Write(msg, 0, 5, 1000, out sentBytes);

            if (sentBytes != 5)
                throw new Exception(string.Format("Failed to send all bytes (result {0})", result));

            var response = (await firstResponse).FirstOrDefault();

            if (response == null)
                throw new TimeoutException("Failed to receive response");

            if (response.Length > 4)
            {
                byte[] resultBuffer = new byte[response.Length - 4];
                Buffer.BlockCopy(response, 4, resultBuffer, 0, resultBuffer.Length);

                return resultBuffer;
            }
            else
                return null;
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

        private void Close()
        {
            lock (this)
            {
                try
                {
                    if (this.usbDevice != null)
                    {
                        if (this.usbDevice.IsOpen)
                        {
                            // Send receiver off
                            try
                            {
                                Task.Run(() => SendCommand(0x14)).Wait();
                            }
                            catch
                            {
                            }

                            if (this.wholeUsbDevice != null)
                            {
                                // Release interface #0
                                wholeUsbDevice.ReleaseInterface(0);

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

            Close();

            UsbDevice.Exit();
        }
    }
}
