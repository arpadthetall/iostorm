using System;
using System.Collections.Generic;
using System.Threading;
using System.Runtime.Serialization;

namespace ZWaveApi.Net
{
    [DataContract]
    public class ZWaveController
    {
        private string _comPort;

        public ZWavePort Port = new ZWavePort();

        public SortedDictionary<byte, ZWaveNode> Nodes { get; private set; }
        public SortedDictionary<byte, Dictionary<byte, CommandClasses.GenericCommandClass>> Commands { get; private set; }
        

        private static List<byte> callbackIds = new List<byte>();

        [DataMember]
        public ZWaveInfo Info { get; private set; }

        public ZWaveController() {
            Nodes = new SortedDictionary<byte, ZWaveNode>();
        }

        public ZWaveController(string port) : this() {
            _comPort = port;
        }


        /// <summary>
        /// If the message is a Request the do some work here.
        ///   - ApplicationUpdate.
        ///   - SendData
        /// </summary>
        /// <param name="sentMessage">The message that we send to the ZWave</param>
        /// <param name="receivedMessage">The message that we received from the ZWave</param>
        private void MessageRequestHandler(Messenger.Message sentMessage, Messenger.Message receivedMessage)
        {
            int length = receivedMessage.buffer.Length;
            CommandClasses.CommandClass commandClass;

            if (length > 3)
                switch (receivedMessage.function)
                {
                    case ZWaveFunction.ApplicationUpdate:
                        switch ((ZWaveApplicationUpdate)receivedMessage.buffer[4])
                        {
                            case ZWaveApplicationUpdate.NODE_INFO_RECEIVED:
                                if (receivedMessage.buffer[6] > 0) {
                                    Nodes[receivedMessage.nodeId].UpdateNodeInfo(receivedMessage);
                                }
                                    //Nodes[receivedMessage.nodeId].AddCommandClass(receivedMessage.buffer);
                                break;
                            case ZWaveApplicationUpdate.NODE_INFO_REQ_FAILED:
                                ZWaveLog.AddEvent("Node info failed " + receivedMessage.nodeId + ";" + ZWavePort.lastSentNodeId);

                                Nodes[ZWavePort.lastSentNodeId].UpdateNodeInfo(null);
                                break;
                            default:
                                break;
                        }

                        break;

                    case ZWaveFunction.SendData:
                        byte nodeId = sentMessage.buffer[4];
                        commandClass = (CommandClasses.CommandClass)sentMessage.buffer[6];

                        CheckAndRemoveCallbackId(receivedMessage.buffer[4]);

                        if (sentMessage.buffer[7] == 0x01)
                            Port.MessagingCompleted();
                        break;

                    case ZWaveFunction.ApplicationCommandHandler:
                        Port.MessagingCompleted();
                        commandClass = Nodes[receivedMessage.nodeId].HandleMessage(receivedMessage);

                        //ZWaveUpdates(receivedMessage.nodeId, commandClass);
                        break;

                    default:
                        Port.MessagingCompleted();
                        break;
                }
        }

        /// <summary>
        /// If the message is a response the do some work here.
        ///   - Discovery nodes.
        ///   - Get node protocol info
        /// </summary>
        /// <param name="sentMessage">The message that we send to the ZWave</param>
        /// <param name="receivedMessage">The message that we received from the ZWave</param>
        private void MessageResponseHandler(Messenger.Message sentMessage, Messenger.Message receivedMessage)
        {
            int length = receivedMessage.buffer.Length;
            byte nodeId;

            if (length > 3)
            {
                Port.MessagingCompleted();
                switch (receivedMessage.function)
                {
                    case ZWaveFunction.GetVersion:
                        Info.getVersion(receivedMessage.buffer);
                        break;

                    case ZWaveFunction.MemoryGetId:
                        Info.MemoryGetId(receivedMessage.buffer);
                        break;

                    case ZWaveFunction.SerialGetCapabilities:
                        Info.SerialGetCapabilities(receivedMessage.buffer);
                        break;

                    case ZWaveFunction.GetControllerCapabilities:
                        Info.GetControllerCapabilities(receivedMessage.buffer);
                        break;

                    case ZWaveFunction.DiscoveryNodes:
                        //CreateNodes(receivedMessage.buffer);
                        ThreadPool.QueueUserWorkItem(new WaitCallback(CreateNodesT), receivedMessage.buffer);

                        break;
                    case ZWaveFunction.SendData:

                        break;

                    case ZWaveFunction.GetNodeProtocolInfo:
                        nodeId = sentMessage.buffer[4];
                        Nodes[nodeId].UpdateNodeProtocolInfo(receivedMessage);
                        break;



                    default:
                        break;

                }
            }

        }

        private void CreateNodesT(object buffer) {
            CreateNodes((byte[])buffer);
        }

        private void CreateNode(object nodeId) {

        }

        /// <summary>
        /// Look at the Message that is received, and Extract nodes.
        /// </summary>
        /// <param name="receivedMessage">The message that we received from the ZWave</param>
        private void CreateNodes(byte[] Buffer) {
            if (Buffer[6] == 29) // 29 bytes = 232 bits, one for each possible node in the network.
            {
                for (int i = 0; i < 29; i++) {
                    for (int j = 0; j < 8; j++) {
                        int nodeId = (i * 8) + j + 1;
                        //ZWaveLog.AddEvent("nid." + nodeId);

                        if ((Buffer[i + 7] & (0x01 << j)) > 0) {
                            ZWaveLog.AddEvent(" create." + nodeId);
                            //ThreadPool.QueueUserWorkItem(new WaitCallback(CreateNodesT), receivedMessage.buffer);

                            lock (Nodes)
                                Nodes.Add((byte)nodeId, new ZWaveNode((byte)nodeId));

                            //Because of the threading, we need the node added to the collection before initialization
                            // Otherwise the callbacks cannot access it, since they rely on the Nodes collection object
                            Nodes[(byte)nodeId].Initialize();
                        }
                    }
                }
            }
        }


        /// <summary>
        /// This handler is call from ZWavePort, and call 
        ///   - MessageRequestHandler if it is a request
        ///   - MessageResponseHandler if it is a Response
        /// </summary>
        /// <param name="sentMessage">The message that we send to the ZWave</param>
        /// <param name="receivedMessage">The message that we received from the ZWave</param>
        protected virtual void MessageHandler(Messenger.Message sentMessage, Messenger.Message receivedMessage)
        {
            if (receivedMessage == null)
            {
                throw new ArgumentNullException("receivedMessage");
            }

            if (sentMessage == null)
            {
                throw new ArgumentNullException("sentMessage");
            }

            int length = receivedMessage.buffer.Length;
            if (receivedMessage.frameHeader != Messenger.FrameHeader.ACK)
            {
                if (length > 2)
                {
                    switch (receivedMessage.messageType)
                    {
                        case Messenger.MessageType.Request:
                            MessageRequestHandler(sentMessage, receivedMessage);
                            break;
                        case Messenger.MessageType.Response:
                            MessageResponseHandler(sentMessage, receivedMessage);
                            break;
                        default:
                            ZWaveLog.AddException("Message type: " + receivedMessage.messageType + " is not defined yet.");
                            break;
                    }
                }

            }
        }

        /// <summary>
        /// Call the Command class, and ask to remove the callbackId.
        /// </summary>
        /// <param name="callbackId">The Callback Id.</param>
        public void CheckAndRemoveCallbackId(byte callbackId)
        {
            // Check the callback id
            if (callbackIds.Contains(callbackId))
            {
                callbackIds.Remove(callbackId); // Remove the callback id
            }
        }

        public bool Open()
        {
            if (Port.Open(this._comPort))
            {
                ZWavePort.MessageHandler messageHandler = new ZWavePort.MessageHandler(MessageHandler);

                Port.SubscribeToMessages(messageHandler);

                Info = new ZWaveInfo();

                // Get Version
                ZWavePort.AddMessage(new Messenger.Message(Messenger.MessageType.Request, ZWaveFunction.GetVersion), ZWaveMessagePriority.Interactive);

                // Get the Home of the Controller
                ZWavePort.AddMessage(new Messenger.Message(Messenger.MessageType.Request, ZWaveFunction.MemoryGetId), ZWaveMessagePriority.Interactive);

                // Get Controller Capabilities
                ZWavePort.AddMessage(new Messenger.Message(Messenger.MessageType.Request, ZWaveFunction.GetControllerCapabilities), ZWaveMessagePriority.Interactive);

                // Get Serial API Capabilities
                ZWavePort.AddMessage(new Messenger.Message(Messenger.MessageType.Request, ZWaveFunction.SerialGetCapabilities), ZWaveMessagePriority.Interactive);

                // Discovery all the nodes in ZWaveApi-
                ZWavePort.AddMessage(new Messenger.Message(Messenger.MessageType.Request, ZWaveFunction.DiscoveryNodes), ZWaveMessagePriority.Interactive);

                System.Threading.Thread.Sleep(10000);

                ZWaveLog.AddEvent("ZWaveApi is started.");

                return true;

            }
            else
            {
                ZWaveLog.AddException("ZWaveApi is not Started.");

                return false;
            }
        }

        public void Close()
        {
            Port.Close();

            ZWaveLog.AddEvent("ZWaveApi is stopped.");
        }
        

    }
}
