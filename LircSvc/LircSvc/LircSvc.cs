using System;
using System.Configuration;
using System.ServiceProcess;
using Ben.LircSharp;
using Qlue.Logging;
using IoStorm;
using IoStorm.Payload;

namespace IoStorm.LircSvc
{
    public partial class LircSvc : ServiceBase
    {
        private static readonly ILogFactory LogFactory = new NLogFactoryProvider();
        private readonly ILog _logger = LogFactory.GetLogger("LircSvc");

        public LircClient Client { get; private set; }
        public bool IsConnected { get; private set; }
        private string LircHost { get; set; }
        private int LircPort { get; set; }
        private string RabbitHost { get; set; }
        private string RabbitChannel { get; set; }
        private string DeviceId { get; set; }

        public LircSvc()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            LoadSettings();
            Connect();
        }

        private void LoadSettings()
        {
            LircHost = ConfigurationManager.AppSettings["LircHost"];
            LircPort = int.Parse(ConfigurationManager.AppSettings["LircPort"]);
            RabbitHost = ConfigurationManager.AppSettings["RabbitHost"];
            RabbitChannel = ConfigurationManager.AppSettings["RabbitChannel"];
            DeviceId = ConfigurationManager.AppSettings["DeviceId"];
        }

        protected override void OnStop()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (Client != null)
            {
                return;
            }

            Client = new LircSocketClient();
            Client.Connected += Client_Connected;
            Client.CommandCompleted += Client_CommandCompleted;
            Client.Error += Client_Error;
            Client.Message += Client_Message;

            _logger.Info("Connecting to {0}:{1}...", LircHost, LircPort);

            Client.Connect(LircHost, LircPort);
        }

        public void Disconnect()
        {
            if (Client == null || !IsConnected) return;

            _logger.Info("Disconnecting from {0}:{1}...", LircHost, LircPort);

            Client.Disconnect();
            Client = null;
            IsConnected = false;

            _logger.Info("Disconnected");
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            IsConnected = true;
            _logger.Info("Connected!");
        }

        private void Client_CommandCompleted(object sender, LircCommandEventArgs e)
        {
            PublishCommand(e.Command.Command);
        }

        private void PublishCommand(string command)
        {
            _logger.Info("Received command: {0}", command);

            var hub = new RemoteHub(LogFactory, RabbitHost, DeviceId);

            var payload = GetPayloadFromLircCommand(command);

            if (payload == null)
            {
                _logger.Warn("No matching payload for command {0}", command);
                return;
            }

            try
            {
                hub.SendPayload(RabbitChannel, payload);
                _logger.Info("Sent {0}", command);
            }
            catch(Exception ex)
            {
                _logger.ErrorException("Failed to send payload", ex);
            }
        }

        private static IPayload GetPayloadFromLircCommand(string command)
        {
            switch (command)
            {
                // Audio
                case LircCommands.Audio.Mute:
                    return new IoStorm.Payload.Audio.MuteToggle();
                case LircCommands.Audio.VolumeUp:
                    return new IoStorm.Payload.Audio.VolumeUp();
                case LircCommands.Audio.VolumeDown:
                    return new IoStorm.Payload.Audio.VolumeDown();

                // Navigation
                case LircCommands.Navigation.Up:
                    return new IoStorm.Payload.Navigation.Up();
                case LircCommands.Navigation.Down:
                    return new IoStorm.Payload.Navigation.Down();
                case LircCommands.Navigation.Left:
                    return new IoStorm.Payload.Navigation.Left();
                case LircCommands.Navigation.Right:
                    return new IoStorm.Payload.Navigation.Right();
                case LircCommands.Navigation.Num0:
                    return new IoStorm.Payload.Navigation.Number0();
                case LircCommands.Navigation.Num1:
                    return new IoStorm.Payload.Navigation.Number1();
                case LircCommands.Navigation.Num2:
                    return new IoStorm.Payload.Navigation.Number2();
                case LircCommands.Navigation.Num3:
                    return new IoStorm.Payload.Navigation.Number3();
                case LircCommands.Navigation.Num4:
                    return new IoStorm.Payload.Navigation.Number4();
                case LircCommands.Navigation.Num5:
                    return new IoStorm.Payload.Navigation.Number5();
                case LircCommands.Navigation.Num6:
                    return new IoStorm.Payload.Navigation.Number6();
                case LircCommands.Navigation.Num7:
                    return new IoStorm.Payload.Navigation.Number7();
                case LircCommands.Navigation.Num8:
                    return new IoStorm.Payload.Navigation.Number8();
                case LircCommands.Navigation.Num9:
                    return new IoStorm.Payload.Navigation.Number9();
                case LircCommands.Navigation.Guide:
                    return new IoStorm.Payload.Navigation.Guide();
                case LircCommands.Navigation.Back:
                    return new IoStorm.Payload.Navigation.Back();
                case LircCommands.Navigation.Enter:
                    return new IoStorm.Payload.Navigation.Enter();
                case LircCommands.Navigation.Home:
                    return new IoStorm.Payload.Navigation.Home();

                // Power
                case LircCommands.Power.Toggle:
                    return new IoStorm.Payload.Power.Toggle();

                // Transport
                case LircCommands.Transport.Advance:
                    return new IoStorm.Payload.Transport.Advance();
                case LircCommands.Transport.FastForward:
                    return new IoStorm.Payload.Transport.FastForward();
                case LircCommands.Transport.Next:
                    return new IoStorm.Payload.Transport.Next();
                case LircCommands.Transport.Pause:
                    return new IoStorm.Payload.Transport.Pause();
                case LircCommands.Transport.Play:
                    return new IoStorm.Payload.Transport.Play();
                case LircCommands.Transport.Previous:
                    return new IoStorm.Payload.Transport.Previous();
                case LircCommands.Transport.Rewind:
                    return new IoStorm.Payload.Transport.Rewind();
                case LircCommands.Transport.Stop:
                    return new IoStorm.Payload.Transport.Stop();

                // TV
                case LircCommands.TV.ChannelUp:
                    return new IoStorm.Payload.TV.ChannelInc();
                case LircCommands.TV.ChannelDown:
                    return new IoStorm.Payload.TV.ChannelDec();
            }

            return null;
        }

        private void Client_Error(object sender, LircErrorEventArgs e)
        {
            _logger.ErrorException(e.Message, e.Exception);
        }

        private void Client_Message(object sender, LircMessageEventArgs e)
        {
            _logger.Info("Message received: {0}", e.Message);
        }
    }
}
