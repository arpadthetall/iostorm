using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.ServiceProcess;
using System.Text;
using Ben.LircSharp;
using NLog;
using NLog.Fluent;
using RabbitMQ.Client;


namespace LircSvc
{
    public partial class LircSvc : ServiceBase
    {
        readonly Logger _logger = LogManager.GetLogger("LircSvc");

        public List<string> SelectedRemoteCommands { get; private set; }
        public LircClient Client { get; private set; }
        public bool IsConnected { get; private set; }
        
        private string LircHost { get; set; }
        private int LircPort { get; set; }
        private string RabbitHost { get; set; }
        private int RabbitPort { get; set; }
        private string RabbitUser { get; set; }
        private string RabbitPass { get; set; }
        private string RabbitQueue { get; set; }

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
            RabbitPort = int.Parse(ConfigurationManager.AppSettings["RabbitPort"]);
            RabbitUser = ConfigurationManager.AppSettings["RabbitUser"];
            RabbitPass = ConfigurationManager.AppSettings["RabbitPass"];
            RabbitQueue = ConfigurationManager.AppSettings["RabbitQueue"];
        }

        protected override void OnStop()
        {
            Disconnect();
        }

        public void Connect()
        {
            if (this.Client != null)
            {
                return;
            }

            this.Client = new LircSocketClient();
            this.Client.Connected += Client_Connected;
            this.Client.CommandCompleted += Client_CommandCompleted;
            this.Client.Error += Client_Error;
            this.Client.Message += Client_Message;

            var message = string.Format("Connecting to {0}:{1}", LircHost, LircPort);
            _logger.Info(message);
            this.Client.Connect(LircHost, LircPort);
        }

        public void Disconnect()
        {
            if (Client == null || !IsConnected) return;

            var message = string.Format("Disconnecting...");
            _logger.Info(message);
            this.Client.Disconnect();
            this.Client = null;
            IsConnected = false;
        }

        private void Client_Connected(object sender, EventArgs e)
        {
            IsConnected = true;
            _logger.Info("Connected!");
        }

        private void Client_CommandCompleted(object sender, LircCommandEventArgs e)
        {
            _logger.Debug(e.Command.Command + " Completed");
            PublishCommand(e.Command.Command);
        }

        private void PublishCommand(string command)
        {
            var factory = new ConnectionFactory
            {
                HostName = RabbitHost,
                Port = RabbitPort,
                UserName = RabbitUser,
                Password = RabbitPass
            };

            using (var connection = factory.CreateConnection())
            {
                using (var channel = connection.CreateModel())
                {
                    channel.QueueDeclare(RabbitQueue, false, false, false, null);

                    var body = Encoding.UTF8.GetBytes(command);

                    channel.BasicPublish("", RabbitQueue, null, body);
                    _logger.Info(" [x] Sent {0}", command);
                }
            }
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
