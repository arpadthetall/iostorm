using System;
using System.Collections.Generic;
using System.Threading;

namespace TidalWaveTest.Devices
{
    abstract class ZWaveNode
    {
        public byte NodeId { get; private set; }
        private readonly ZWavePort _zp;

        protected List<byte> CallbackIds = new List<byte>();

        protected ZWaveNode(byte nodeId, ZWavePort zp)
        {
            NodeId = nodeId;
            _zp = zp;
            var messageHandler = new ZWavePort.MessageHandler(MessageHandler);
            zp.SubscribeToMessages(messageHandler);
        }

        protected Boolean SendMessage(byte[] message)
        {
            byte callbackId = _zp.GetCallbackId(); // Retrieve a callback id from the ZWavePort object
            CallbackIds.Add(callbackId); // Add callback id to the list
            message[message.Length - 2] = callbackId; // Insert the callback id into the message
            while (!_zp.SendMessage(message)) Thread.Sleep(100);
            return true;
        }


        protected void MessagingCompleted(byte callbackId)
        {
            CallbackIds.Remove(callbackId); // Remove the callback id
            _zp.MessagingCompleted();
        }

        public virtual void MessageHandler(byte[] message)
        {
            if (message[0] == 0x06) return;

            // Check the callback id
            var callbackId = message[4];
            if ((message.Length != 7) || !CallbackIds.Contains(callbackId)) return;

            Console.WriteLine(GetType().Name + " (node id: " + NodeId + "): Messaging completed");
            MessagingCompleted(callbackId);
        }
    }
}
