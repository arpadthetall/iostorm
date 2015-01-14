using System;
using System.Configuration;
using System.ServiceProcess;
using Ben.LircSharp;
using Qlue.Logging;
using Storm;
using Storm.Payload;

namespace LircSvc
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
                    return new Storm.Payload.Audio.MuteToggle();
                case LircCommands.Audio.VolumeUp:
                    return new Storm.Payload.Audio.VolumeUp();
                case LircCommands.Audio.VolumeDown:
                    return new Storm.Payload.Audio.VolumeDown();

                // Navigation
                case LircCommands.Navigation.Up:
                    return new Storm.Payload.Navigation.Up();
                case LircCommands.Navigation.Down:
                    return new Storm.Payload.Navigation.Down();
                case LircCommands.Navigation.Left:
                    return new Storm.Payload.Navigation.Left();
                case LircCommands.Navigation.Right:
                    return new Storm.Payload.Navigation.Right();
                case LircCommands.Navigation.Num0:
                    return new Storm.Payload.Navigation.Number0();
                case LircCommands.Navigation.Num1:
                    return new Storm.Payload.Navigation.Number1();
                case LircCommands.Navigation.Num2:
                    return new Storm.Payload.Navigation.Number2();
                case LircCommands.Navigation.Num3:
                    return new Storm.Payload.Navigation.Number3();
                case LircCommands.Navigation.Num4:
                    return new Storm.Payload.Navigation.Number4();
                case LircCommands.Navigation.Num5:
                    return new Storm.Payload.Navigation.Number5();
                case LircCommands.Navigation.Num6:
                    return new Storm.Payload.Navigation.Number6();
                case LircCommands.Navigation.Num7:
                    return new Storm.Payload.Navigation.Number7();
                case LircCommands.Navigation.Num8:
                    return new Storm.Payload.Navigation.Number8();
                case LircCommands.Navigation.Num9:
                    return new Storm.Payload.Navigation.Number9();
                case LircCommands.Navigation.Guide:
                    return new Storm.Payload.Navigation.Guide();
                case LircCommands.Navigation.Back:
                    return new Storm.Payload.Navigation.Back();
                case LircCommands.Navigation.Enter:
                    return new Storm.Payload.Navigation.Enter();
                case LircCommands.Navigation.Home:
                    return new Storm.Payload.Navigation.Home();

                // Power
                case LircCommands.Power.Toggle:
                    return new Storm.Payload.Power.Toggle();

                // Transport
                case LircCommands.Transport.Advance:
                    return new Storm.Payload.Transport.Advance();
                case LircCommands.Transport.FastForward:
                    return new Storm.Payload.Transport.FastForward();
                case LircCommands.Transport.Next:
                    return new Storm.Payload.Transport.Next();
                case LircCommands.Transport.Pause:
                    return new Storm.Payload.Transport.Pause();
                case LircCommands.Transport.Play:
                    return new Storm.Payload.Transport.Play();
                case LircCommands.Transport.Previous:
                    return new Storm.Payload.Transport.Previous();
                case LircCommands.Transport.Rewind:
                    return new Storm.Payload.Transport.Rewind();
                case LircCommands.Transport.Stop:
                    return new Storm.Payload.Transport.Stop();

                // TV
                case LircCommands.TV.ChannelUp:
                    return new Storm.Payload.TV.ChannelInc();
                case LircCommands.TV.ChannelDown:
                    return new Storm.Payload.TV.ChannelDec();
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
