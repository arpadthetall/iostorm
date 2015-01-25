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

using System;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;
using ZWaveApi.Net.Messenger;

namespace ZWaveApi.Net
{
    /// <summary>
    /// The ZWavePort class will be responsible for the basic communication and protocol handling
    /// </summary>
    public class ZWavePort
    {
        static SerialPort sp = new SerialPort();

        public delegate void MessageHandler(Message sentMessage, Message receivedMessage);
        protected MessageHandler messageHandler;
        private Message sentMessage = new Message();

        private Thread receiverThread;
        private Thread senderThread;

        private Object messagingLocker = new Object();
        private Boolean messagingLock;

        private static byte callbackIdPool = 0;
        private static Object callbackIdLock = new Object();
        public static byte lastSentNodeId = 0;

        private static PriorityQueue<ZWaveMessage, ZWaveMessagePriority> queue = new PriorityQueue<ZWaveMessage, ZWaveMessagePriority>();

        /// <summary>
        /// Set the default parameter, to the serial, specific by ZWave.
        /// The defalut Port is COM4
        /// </summary>
        public ZWavePort()
        {
            try
            {
                receiverThread = new Thread(new System.Threading.ThreadStart(ReceiveMessage));
                receiverThread.Start();
                ZWaveLog.AddEvent("The Thread ReceiveMessage is started");
            }
            catch
            {
                ZWaveLog.AddException("Can´t start the thread ReceiveMessage");
            }

            try
            {
                senderThread = new Thread(new System.Threading.ThreadStart(SendMessage));
                senderThread.Start();
                ZWaveLog.AddEvent("The Thread SendMessage is started");
            }
            catch
            {
                ZWaveLog.AddException("Can´t start the thread SendMessage");
            }


        }

        /// <summary>
        /// Closer the Serialport and the 2 thread ( sender and receiver)
        /// </summary>
        ~ZWavePort()
        {
            if (sp.IsOpen == true)
            {
                this.Close();

                receiverThread.Abort();
                ZWaveLog.AddEvent("The Thread ReceiveMessage is stopped");

                senderThread.Abort();
                ZWaveLog.AddEvent("The Thread SendMessage is Stopped");

            }
        }

        /// <summary>
        /// Open the Seriel Port, with the default parameter, that is set by the contrunct.
        /// The Port is only open it´s close.
        /// </summary>
        public bool Open()
        {
            if (!sp.IsOpen)
            {
                try
                {
                    sp.BaudRate = 115200;
                    sp.Parity = Parity.None;
                    sp.DataBits = 8;
                    sp.StopBits = StopBits.One;
                    sp.Handshake = Handshake.None;
                    sp.DtrEnable = true;
                    sp.RtsEnable = true;
                    sp.NewLine = System.Environment.NewLine;
                    sp.Open();

                    ZWaveLog.AddEvent("The Com " + sp.PortName + " is opened and ready.");

                    if (sp.IsOpen)
                    {
                        // Send NAX to ZWave.
                        sp.Write(new byte[] { (byte)FrameHeader.NAK }, 0, 1);

                        return true;
                    }
                }

                catch (Exception ex)
                {
                    ZWaveLog.AddException(ex);
                }
            }

            return false;
        }

        /// <summary>
        /// Set the Portname, and open the Serial Port
        /// </summary>
        /// <param name="PortName">The Name of the Port fx. "COM4"</param>
        public bool Open(string comPort)
        {
            if (!sp.IsOpen)
            {
                sp.PortName = comPort;
                return Open();
            }
            else
                return true;
        }

        /// <summary>
        /// Checker if the Serial Port is open
        /// </summary>
        /// <returns></returns>
        public bool IsOpen()
        {
            return sp.IsOpen;
        }

        /// <summary>
        /// Close the Serial port if it is open.
        /// </summary>
        public void Close()
        {
            if (sp.IsOpen)
                sp.Close();

            ZWaveLog.AddEvent("The ComPort is close.");
        }

        /// <summary>
        /// Add A message handler to this class message handler.
        /// </summary>
        /// <param name="messageHandler">The message handler that is to be add</param>
        public void SubscribeToMessages(MessageHandler messageHandler)
        {
            this.messageHandler += messageHandler;
        }

        /// <summary>
        /// See if there is at message i the queue, and send it to the ZWave Controller.
        /// </summary>
        private void SendMessage()
        {
            while (true)
            {
                if (sp.IsOpen)
                {
                    ZWaveMessage message = null;

                    while (queue.Count == 0) Thread.Sleep(100); // Loop until the queue has elements
                    while ((queue.Peek().Value.buffer[0] != (byte)FrameHeader.ACK) && (queue.Peek().Value.buffer[0] != (byte)FrameHeader.NAK) && (!SetMessagingLock(true)))
                    {
                        Thread.Sleep(100);
                    }

                    message = queue.Dequeue().Value;

                    if (message.buffer[0] != (byte)FrameHeader.ACK)
                    {
                        sentMessage.buffer = message.buffer;
                    }

                    ZWaveLog.addMessageBuffer("Sent     : ", message.buffer);

                    try
                    {
                        sp.Write(message.buffer, 0, message.buffer.Length);
                        if (message.buffer.Length > 4)
                            lastSentNodeId = message.buffer[4];

                    }
                    catch (Exception e)
                    {
                            ZWaveLog.AddException(e);

                    }
                }
            }
        }

        /// <summary>
        /// Receive Message from the ZWave Controller, 
        /// and send a Ack back, if that is reque.
        /// </summary>
        private void ReceiveMessage()
        {
            while (true)
            {
                if (sp.IsOpen && messageHandler != null)
                {
                    try
                    {
                        int cmd = sp.ReadByte();

                        if (cmd != (int)FrameHeader.ACK) {
                            int lenghtcmd = sp.ReadByte();

                            byte[] buffer = new byte[lenghtcmd + 2];  // The lenght af buffer from Zwave.
                            buffer[0] = (byte)cmd;
                            buffer[1] = (byte)lenghtcmd;
                            for (int i = 2; i < lenghtcmd + 2; i++) {
                                buffer[i] = (byte)sp.ReadByte();
                            }


                            ZWaveLog.addMessageBuffer("Received : ", buffer);

                            if (buffer[lenghtcmd + 1] == GenerateChecksum(buffer)) {
                                // Checksum correct - send ACK
                                SendAck();
                                messageHandler(sentMessage, new Message(buffer));
                            } else {
                                ZWaveLog.AddEvent("Checksum incorrect - sending NAK (G: " + GenerateChecksum(buffer) + "b[" + lenghtcmd + "+1]: " + buffer[lenghtcmd + 1] + ")");
                                SendNak();
                            }



                        } else {
                            ZWaveLog.AddEvent("Received : " + cmd.ToString("X2"));
                        }
                    } catch (Exception e) {
                        ZWaveLog.AddException(e);
                    }
                }
            }
        }

        private static byte GenerateChecksum(byte[] data) {
            int offset = 1;
            byte ret = data[offset];
            for (int i = offset + 1; i < data.Length - 1; i++) {
                // Xor bytes
                ret ^= data[i];
            }
            // Not result
            ret = (byte)(~ret);
            return ret;
        }

        /// <summary>
        /// Send the ACK message to the Controller.
        /// </summary>
        private void SendAck()
        {
            ZWavePort.AddMessage(new Message(FrameHeader.ACK), ZWaveMessagePriority.Control);
        }

        /// <summary>
        /// Send the NAK message to the Controller.
        /// </summary>
        private void SendNak() {
            ZWavePort.AddMessage(new Message(FrameHeader.NAK), ZWaveMessagePriority.Control);
        }

        /// <summary>
        /// Add a message to the queue.
        /// </summary>
        /// <param name="message"></param>
        public static void AddMessage(Message message, ZWaveMessagePriority priority)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            queue.Enqueue(new ZWaveMessage(message.Assembly()), priority);
        }

        /// <summary>
        /// The Messaging is Completed.
        /// </summary>
        public void MessagingCompleted()
        {
            SetMessagingLock(false);
        }

        /// <summary>
        /// Is the messege quece send
        /// </summary>
        /// <param name="state">the stat of the message</param>
        /// <returns>True if the message is send, else False.</returns>
        private Boolean SetMessagingLock(Boolean state)
        {
            lock (messagingLocker)
            {
                if (state)
                {
                    if (this.messagingLock)
                    {
                        return false;
                    }
                    else
                    {
                        this.messagingLock = true;
                        return true;
                    }
                }
                else
                {
                    this.messagingLock = false;
                    return true;
                }
            }
        }

        /// <summary>
        /// Count the callbackIdPool one up.
        /// </summary>
        /// <returns>The new callbackId</returns>
        public static byte GetCallbackId()
        {
            lock (callbackIdLock)
            {
                return ++callbackIdPool;
            }
        }


    }
}
