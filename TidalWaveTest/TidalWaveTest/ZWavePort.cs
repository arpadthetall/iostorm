using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading;

namespace TidalWaveTest
{
    class ZWavePort
    {
        public delegate void MessageHandler(byte[] message);
        private readonly Object _messagingLocker = new Object();
        private readonly Object _callbackIdLock = new Object();
        private readonly byte[] _msgAcknowledge = { 0x06 };
        private readonly Thread _receiverThread;
        private MessageHandler _messageHandler;
        private readonly SerialPort _sp;
        private Boolean _sendAck = true;
        private Boolean _messagingLock;
        private byte _callbackIdPool;
        
        public ZWavePort()
        {
            _sp = new SerialPort
            {
                PortName = "COM4",
                BaudRate = 115200,
                Parity = Parity.None,
                DataBits = 8,
                StopBits = StopBits.One,
                Handshake = Handshake.None,
                DtrEnable = true,
                RtsEnable = true,
                NewLine = Environment.NewLine
            };

            _receiverThread = new Thread(ReceiveMessage);
        }

        public void Open()
        {
            if (_sp.IsOpen == false)
            {
                _sp.Open();
                _receiverThread.Start();
            }
        }

        public Boolean SendMessage(byte[] message)
        {
            if (_sp.IsOpen)
            {
                if (message[0] != 0x06)
                {
                    if (!SetMessagingLock(true)) return false;
                    _sendAck = false;
                    message[message.Length - 1] = GenerateChecksum(message); // Insert checksum
                }
                Console.WriteLine("Message sent: " + ByteArrayToString(message));
                _sp.Write(message, 0, message.Length);
                return true;
            }
            return false;
        }

        private void SendAckMessage()
        {
            SendMessage(_msgAcknowledge);
        }

        public void Close()
        {
            if (_sp.IsOpen)
            {
                _sp.Close();
            }
        }

        private static byte GenerateChecksum(byte[] data)
        {
            const int offset = 1;
            var ret = data[offset];
            for (var i = offset + 1; i < data.Length - 1; i++)
            {
                // Xor bytes
                ret ^= data[i];
            }
            // Not result
            ret = (byte)(~ret);
            return ret;
        }

        private void ReceiveMessage()
        {
            while (_sp.IsOpen)
            {
                var bytesToRead = _sp.BytesToRead;
                if (!((bytesToRead != 0) & (_sp.IsOpen))) continue;

                var message = new byte[bytesToRead];
                _sp.Read(message, 0, bytesToRead);

                Console.WriteLine("Message received: " + ByteArrayToString(message));

                if (_sendAck) // Does the incoming message require an ACK?
                {
                    SendAckMessage();
                }

                _sendAck = true;

                if (_messageHandler != null) _messageHandler(message);
            }
        }

        public void SubscribeToMessages(MessageHandler handler)
        {
            _messageHandler += handler;
        }

        private Boolean SetMessagingLock(Boolean state)
        {
            lock (_messagingLocker)
            {
                if (state)
                {
                    if (_messagingLock)
                    {
                        return false;
                    }

                    _messagingLock = true;
                    return true;
                }

                _messagingLock = false;
                return true;
            }
        }

        public void MessagingCompleted()
        {
            SetMessagingLock(false);
        }

        public byte GetCallbackId()
        {
            lock (_callbackIdLock)
            {
                return ++_callbackIdPool;
            }
        }

        private static String ByteArrayToString(IEnumerable<byte> message)
        {
            var ret = message.Aggregate(String.Empty, (current, b) => current + (b.ToString("X2") + " "));
            return ret.Trim();
        }
    }
}
