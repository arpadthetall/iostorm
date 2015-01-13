using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.RemoteMapping.StandardFunctions
{
    public static class Standard
    {
        public static Func<Payload.IPayload> GetPayload(StandardCommands.Commands input)
        {
            switch (input)
            {
                case StandardCommands.Commands.Enter:
                    return () => new Payload.Navigation.Enter();

                case StandardCommands.Commands.Number1:
                    return () => new Payload.Navigation.Number1();

                case StandardCommands.Commands.Number2:
                    return () => new Payload.Navigation.Number2();

                case StandardCommands.Commands.Number3:
                    return () => new Payload.Navigation.Number3();

                case StandardCommands.Commands.Number4:
                    return () => new Payload.Navigation.Number4();

                case StandardCommands.Commands.Number5:
                    return () => new Payload.Navigation.Number5();

                case StandardCommands.Commands.Number6:
                    return () => new Payload.Navigation.Number6();

                case StandardCommands.Commands.Number7:
                    return () => new Payload.Navigation.Number7();

                case StandardCommands.Commands.Number8:
                    return () => new Payload.Navigation.Number8();

                case StandardCommands.Commands.Number9:
                    return () => new Payload.Navigation.Number9();

                case StandardCommands.Commands.Number0:
                    return () => new Payload.Navigation.Number0();
            }

            return null;
        }
    }
}
