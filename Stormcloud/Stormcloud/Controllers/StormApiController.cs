using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Configuration;
using System.Web.Http;
using IoStorm;
using IoStorm.Payload;
using Qlue.Logging;

namespace Stormcloud.Controllers
{
    public class KeyCommand
    {
        public string Command { get; set; }
        public string DeviceId { get; set; }
        public string Level { get; set; }
    }

    public class StormApiController : ApiController
    {
        private static readonly ILogFactory LogFactory = new NLogFactoryProvider();
        private readonly ILog _logger = LogFactory.GetLogger("LircSvc");

        [HttpPost]
        public void PostKeyCode([FromBody]KeyCommand command)
        {
            var rabbitHost = WebConfigurationManager.AppSettings["RabbitHost"];
            var rabbitChannel = WebConfigurationManager.AppSettings["RabbitChannel"];
            var deviceId = IoStorm.Addressing.HubAddress.FromString(WebConfigurationManager.AppSettings["DeviceId"]);
            //_logger.Info("Received command: {0}", command);

            var hub = new RemoteHub(LogFactory, rabbitHost, deviceId);

            var payload = GetPayloadFromLircCommand(command.Command, command.DeviceId);

            if (payload == null)
            {
                //_logger.Warn("No matching payload for command {0}", command);
                return;
            }

            try
            {
                hub.SendPayload(payload);
                //_logger.Info("Sent {0}", command);
            }
            catch (Exception /*ex*/)
            {
                //_logger.ErrorException("Failed to send payload", ex);
            }
        }

        private static IPayload GetPayloadFromLircCommand(string command, string deviceId)
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

                case LircCommands.Light.Off:
                    return new IoStorm.Payload.Light.Off { LightId = deviceId };
                case LircCommands.Light.On:
                    return new IoStorm.Payload.Light.On { LightId = deviceId };

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
    }
}
