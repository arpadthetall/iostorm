using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Storm.RemoteMapping.StandardCommands
{
    public class Sony12
    {
        private int address;
        private int command;

        public Sony12(int address, int command)
        {
            this.address = address;
            this.command = command;
        }

        public Commands? StandardCommand
        {
            get
            {
                switch (this.command)
                {
                    case 0:
                        return Commands.Number1;

                    case 1:
                        return Commands.Number2;

                    case 2:
                        return Commands.Number3;

                    case 3:
                        return Commands.Number4;

                    case 4:
                        return Commands.Number5;

                    case 5:
                        return Commands.Number6;

                    case 6:
                        return Commands.Number7;

                    case 7:
                        return Commands.Number8;

                    case 8:
                        return Commands.Number9;

                    case 9:
                        return Commands.Number0;

                    case 16:
                        return Commands.ChannelInc;

                    case 17:
                        return Commands.ChannelDec;

                    case 18:
                        return Commands.VolumeInc;

                    case 19:
                        return Commands.VolumeDec;
                }

                return null;
            }
        }

        private Func<Payload.IPayload> GetFunctionTV()
        {
            //switch (this.command)
            //{
            //}

            return null;
        }

        private Func<Payload.IPayload> GetFunction151()
        {
            switch (this.command)
            {
                case 29:
                    return () => new Payload.Navigation.Period();
            }

            return null;
        }

        public Func<Payload.IPayload> GetPayload()
        {
            Commands? standardCommand = StandardCommand;

            if (standardCommand.HasValue)
            {
                var standardFunction = StandardFunctions.Standard.GetPayload(standardCommand.Value);

                if (standardFunction != null)
                    return standardFunction;
            }

            Func<Payload.IPayload> func = null;

            switch (this.address)
            {
                case 1:
                    func = GetFunctionTV();
                    break;

                case 151:
                    func = GetFunction151();
                    break;
            }

            return func;
        }

        public override string ToString()
        {
            return string.Format("A{0} C{1}", this.address, this.command);
        }
    }
}
