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
using System.Runtime.Serialization;
using System.Collections.Generic;
using System.Configuration;
using ZWaveApi.Net;
using ZWaveApi.Net.CommandClasses;
using ZWaveApi.Net.Messenger;
using System.Threading;

namespace ZWaveApi.Net
{
    /// <summary>
    /// The class the the nodes.
    /// </summary>
    [DataContract]
    public class ZWaveNode
    {
        private byte basicType;

        public CommandClasses.Version Versions { get; private set; }
        private CommandClasses.MultiInstance MultiInstance { get; set; }
        private CommandClasses.WakeUp WakeUp { get; set; }

		private List<InitializationState> _completedInitialization;  
		static EventWaitHandle _initWaitHandle = new AutoResetEvent (false);  
		private readonly object _locker = new object();

        [DataMember]
        private Dictionary<CommandClass, int> _commands;

        private Dictionary<Utilities.CommandIndex, CommandClasses.GenericCommandClass> _commandClasses;

        [DataMember]
        public Dictionary<Utilities.CommandIndex, CommandClasses.GenericCommandClass> CommandClasses {
            get { return this._commandClasses; } 
        }

        [DataMember]
        public List<CommandClasses.GenericCommandClass> ListOfCommandClasses
        {
            get { return new List<CommandClasses.GenericCommandClass>(_commandClasses.Values); }
        }

        //private SortedDictionary<CommandClass, GenericCommandClass> listOfCommandClass = new SortedDictionary<CommandClass, GenericCommandClass>();

        [DataMember]
        public byte NodeId { get; private set; }

        [DataMember]
        public GenericType GenericType { get; private set; }

        [DataMember]
        public int Version { get; private set; }

        [DataMember]
        public int maxBaudRate { get; private set; }

        [DataMember]
        public int Security { get; private set; }

        [DataMember]
        public Boolean Listening { get; private set; }

        [DataMember]
        public Boolean Routing { get; private set; }


        [DataMember]
        public uint HomeID { get; private set; }             // The HomeID of the controller.

        [DataMember]
        public Boolean Initializing { get; private set; }
        

        public ZWaveNode()
        {
            _completedInitialization = new List<InitializationState>();
            _commands = new Dictionary<CommandClass, int>();
            this._commandClasses = new Dictionary<Utilities.CommandIndex, GenericCommandClass>();
        }

        /// <summary>
        /// Create e a default node with som parameters.
        /// </summary>
        public ZWaveNode(byte nodeId)
            : this()
        {
			
            this.NodeId = nodeId;
            this.Versions = null;
            this.MultiInstance = null;
            this.WakeUp = null;
            ZWaveLog.AddEvent("Created node with id: " + nodeId);
        }

		
		public void Initialize() {

            lock (this._locker) 
            {
                if (this.Initializing == true || this._completedInitialization.Contains(InitializationState.Complete)) {
                    return;
                }
            }

            try {
                lock (this._locker)
                    this.Initializing = true;

                ZWaveLog.AddEvent("Initialize!" + NodeId);

                if (!this._completedInitialization.Contains(InitializationState.ProtocolInfo)) {

                    //Request node protocol info
                    ZWaveLog.AddEvent("Send node protocol info request" + NodeId);
                    ZWavePort.AddMessage(new Message(MessageType.Request, ZWaveFunction.GetNodeProtocolInfo, (byte)this.NodeId), ZWaveMessagePriority.Interactive);

                    //Wait for response
                    _initWaitHandle.WaitOne();
                    ZWaveLog.AddEvent("Returned from response" + NodeId);
                    if (this.basicType == (byte)GenericType.StaticController) {
                        ZWaveLog.AddEvent("Is static controller, so leave initialization now");
                        return;
                    }

                    //TODO: Load information from cache!
                    // Proposal, be xml compatible with Open-ZWave
                }

                if (!this._completedInitialization.Contains(InitializationState.WakeUp)) {
                    // Check if we through xml loaded the WakeUpClass
                    // but we should check for the Node.Listening (true/false) if false then we are dealing with a WakeUp node!
                    // If we have not added the WakeUp from cache, add it now
                    if (!this.Listening) {
                        //if (_commands.ContainsKey(CommandClass.WakeUp)) {
                            //Add commandclass wakeup with instance 0 (since wakeups will never be multiinstance?)

                            this.WakeUp = new CommandClasses.WakeUp(this, (byte)0x00);

                        //}
                        //Do stuff to make sure we can handle a wakeup node
                    }
                }


                ZWaveLog.AddEvent("Before: Send node info request");
                if (!this._completedInitialization.Contains(InitializationState.NodeInfo)) {
                    ZWaveLog.AddEvent("Send node info request" + this.NodeId);
                    ZWavePort.AddMessage(new Message(MessageType.Request, ZWaveFunction.RequestNodeInfo, (byte)this.NodeId), ZWaveMessagePriority.Status);

                    //Wait for response of completed NodeInfo
                    _initWaitHandle.WaitOne();
                    if (this._completedInitialization.Contains(InitializationState.NodeInfo)) {
                    
                      
 
                        ZWaveLog.AddEvent("Returned from response: success");
                    } else {
                        ZWaveLog.AddEvent("Returned from response: failure (most likely it is offline, and not a wakeup device)");
                      //  return;
                    }
                }

                if (!this._completedInitialization.Contains(InitializationState.ManufacturerSpecific)) {
                    if (_commands.ContainsKey(CommandClass.ManufacturerSpecific)) {
                        //TODO: Handle manufacturer specific information (i.e. overwrite _commands)
                        // Proposal, be xml compatible with Open-ZWave
                    }
                }

                if (!this._completedInitialization.Contains(InitializationState.Versions)) {
                    if (this._commands.ContainsKey(CommandClass.Version)) {
                        this.Versions = new CommandClasses.Version(this, 0);
                        this._commands.Remove(CommandClass.Version);

                        foreach (CommandClass c in _commands.Keys) {
                            ZWaveLog.AddEvent(c + " get version ");
                            this.Versions.RequestCommandClassVersion(c);
                            _initWaitHandle.WaitOne();
                            ZWaveLog.AddEvent(c + " is version " + this.Versions.CommandClassVersion);
                        }
                    }

                    this._completedInitialization.Add(InitializationState.Versions);
                }

                if (!this._completedInitialization.Contains(InitializationState.Instances)) {
                    if (_commands.ContainsKey(CommandClass.MultiInstance)) {
                        this.MultiInstance = new CommandClasses.MultiInstance(this, (byte)0x00);

                        this._commands.Remove(CommandClass.MultiInstance);

                        CommandClass[] keys = new CommandClass[_commands.Keys.Count];
                        _commands.Keys.CopyTo(keys, 0);
                        foreach (CommandClass c in keys) {
                            ZWaveLog.AddEvent(c + " get instances ");
                            this.MultiInstance.RequestCommandClassInstances(c);
                            _initWaitHandle.WaitOne();
                            ZWaveLog.AddEvent(c + " has instances " + this.MultiInstance.CommandInstances(c));
                            if (this.MultiInstance.CommandInstances(c) > 1) {
                                for (int i = 1; i <= this.MultiInstance.CommandInstances(c); i++) {
                                    AddCommandClass(c, i);
                                }
                            } else {
                                AddCommandClass(c, 0);
                            }
                        }
                    } else {
                        CommandClass[] keys = new CommandClass[_commands.Keys.Count];
                        _commands.Keys.CopyTo(keys, 0);

                        foreach (CommandClass c in keys) {
                            AddCommandClass(c, 0);
                        }
                    }

                    this._completedInitialization.Add(InitializationState.Instances);
                }

                if (!this._completedInitialization.Contains(InitializationState.Static)) {
                }

                if (!this._completedInitialization.Contains(InitializationState.Associations)) {

                }


                if (!this._completedInitialization.Contains(InitializationState.Neighbors)) {

                }

                if (!this._completedInitialization.Contains(InitializationState.Session)) {

                }

                if (!this._completedInitialization.Contains(InitializationState.Dynamic)) {

                }

                if (!this._completedInitialization.Contains(InitializationState.Configuration)) {

                }


                if (!this._completedInitialization.Contains(InitializationState.Complete)) {

                    //TODO: Save information to cache!

                    this._completedInitialization.Add(InitializationState.Complete);
                }

            } finally {

                lock (this._locker)
                    this.Initializing = false;
            }
			
		}

        private void AddCommandClass(CommandClass Command, int Instance)
        {
            try {
                if (Command != CommandClass.WakeUp) {
                    string className = "ZWaveApi.Net.CommandClasses." + Command;

                    GenericCommandClass gcc = (GenericCommandClass)Activator.CreateInstance(Type.GetType(className), new object[] { this, (byte)Instance });
                    this._commandClasses.Add(new Utilities.CommandIndex(this.NodeId, Command, (byte)Instance), gcc);
                    this._commands.Remove(Command);
                    ZWaveLog.AddEvent(Command + " is created, with nodeid " + this.NodeId + " and instance " + Instance);
                }
            } catch (Exception e) {
                ZWaveLog.AddException(e.Message);
                ZWaveLog.AddException(Command + " is not defined yet." + Command.ToString("X"));
            }
             
        }
		
		public void UpdateNodeProtocolInfo(Message receivedMessage) {

            lock (this._locker) {
                if (this._completedInitialization.Contains(InitializationState.ProtocolInfo)) {
                    // Get state for all command classes on wake up
                    foreach (CommandClasses.GenericCommandClass command in this._commandClasses.Values) {
                        Console.WriteLine("Request state: " + command.CommandClass);
                        command.RequestState();
                    }
                    return;
                }
            }

            ZWaveLog.AddEvent("Got GetNodeProtocolInfo response" + NodeId);

			//Handle the WaveFunction.GetNodeProtocolInfo

            this.basicType = receivedMessage.buffer[7];
            this.GenericType = (GenericType)receivedMessage.buffer[8];

            this._commands.Add(CommandClass.Basic,1);
            this.AddCommandClass(CommandClass.Basic, 0x00);

            ZWaveLog.AddEvent("Basic Type for " + NodeId + ": " + this.basicType);
            ZWaveLog.AddEvent("Generic Type for " + NodeId + ": " + this.GenericType);
            this.Version = (receivedMessage.buffer[9] & 0x07) + 1;

            this.Listening = ((receivedMessage.buffer[9] & 0x80) != 0);
            this.Routing = ((receivedMessage.buffer[9] & 0x40) != 0);

            this.maxBaudRate = 9600;
            if ((receivedMessage.buffer[9] & 0x38) == 0x10)
            {
                this.maxBaudRate = 40000;
            }
            this.Security = (receivedMessage.buffer[10] & 0x7f);

			//TODO: Investigate bytes: (11), 12, 13, 14 = basic/generic/specific

            this.CompleteInitializationStep(InitializationState.ProtocolInfo);

        }

        public void UpdateNodeInfo(Message receivedMessage) {
            ZWaveLog.AddEvent("Got GetNodeInfo response" + NodeId);

            lock (this._locker) {
                if (this._completedInitialization.Contains(InitializationState.NodeInfo)) {
                    return;
                }
            }

            if (receivedMessage == null) {
                _initWaitHandle.Set();
                return;
            }


            if (receivedMessage.buffer == null) {
                throw new ArgumentNullException("message");
            }

            if (receivedMessage.buffer[5] == this.NodeId) {
                byte[] tmp = new byte[receivedMessage.buffer.Length - 11];
                System.Array.Copy(receivedMessage.buffer, 10, tmp, 0, receivedMessage.buffer.Length - 11);

                foreach (byte b in tmp) {
                    if (b != 0x00) {

                        if (b == (byte)CommandClass.Mark) {
                            //Any command classes after the Mark, are classes that can be controlled by the node
                            ZWaveLog.AddEvent("Reached MARK, exit from create commandclasses for nodeid " + this.NodeId);
                            break;
                        }

                        ZWaveLog.AddEvent("Added command " + (CommandClass)b + " for node " + this.NodeId);
                        _commands.Add((CommandClass)b, 1);
                    }
                }

                //RequestStateAll();
            }

            this.CompleteInitializationStep(InitializationState.NodeInfo);
           
        }

        private void InitializeThreadStart(Object stateInfo) {
            this.Initialize();
        }

        private void CompleteInitializationStep(InitializationState state) {

            bool init;

            lock (this._locker) {
                init = this.Initializing;
                this._completedInitialization.Add(state);
            }

            if (!init) {
                // We are in a response process so we need to put the initialization in another thread:
                //ThreadPool.QueueUserWorkItem(new WaitCallback(this.InitializeThreadStart));
            } else {
                //Continue with initialization
                _initWaitHandle.Set();
            }
        }

        public int GetCommandVersion(CommandClass command) {
            
            if (this.Versions != null) {
                return Versions.CommandVersion(command);
            } else {
                return 0;
            }
        }

        public CommandClass HandleMessage(Message receivedMessage)
        {
            CommandClass commandClass = (CommandClass)receivedMessage.buffer[7];

            try {

                switch (commandClass) {
                    case CommandClass.Version:
                        this.Versions.HandleMessage(receivedMessage);

                        if (!_completedInitialization.Contains(InitializationState.Versions)) {
                            _initWaitHandle.Set();
                        }
                        break;
                    case CommandClass.MultiInstance:
                        this.MultiInstance.HandleMessage(receivedMessage);

                        if (!_completedInitialization.Contains(InitializationState.Instances)) {
                            _initWaitHandle.Set();
                        }
                        break;
                    case CommandClass.WakeUp:
                        this.WakeUp.HandleMessage(receivedMessage);

                        break;
                    default:
                        CommandClasses.GenericCommandClass command;

                        if (this._commandClasses.TryGetValue(new Utilities.CommandIndex(this.NodeId, commandClass, receivedMessage.instanceId), out command))
                        {
                            command.HandleMessage(receivedMessage);
                        } else {
                            throw new Exception("Command class " + commandClass + " not loaded");
                        }
                        break;

                }
            } catch (Exception e) {
                ZWaveLog.AddException(e);
            }


            return commandClass;
        }

        public void SendMessage(GenericCommandClass commandClass, Message message, ZWaveMessagePriority priority) {
            //TODO: If sleeping send to WakeUp Command Class - which will handle queing of messages, and send of messages on WakeUp notification
            if (!this.Listening) {

            } 

            //TODO: If command class is multiinstance, send to MultiInstanceCommand class which will handle encapsulation
            if (this.MultiInstance != null && commandClass.InstanceId > 0) {
                message = this.MultiInstance.EncapsulateMessage(commandClass, message);
            } 

            ZWavePort.AddMessage(message, priority);
            
        }


    }
}