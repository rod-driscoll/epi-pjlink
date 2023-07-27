using System;
using PepperDash.Core;
using PepperDash.Essentials.Core.Queues;

namespace EpsonProjectorEpi
{
    public static class Commands
    {
        public class EpsonCommand : IQueueMessage
        {
            public IBasicCommunication Coms { get; set; }
            public string Message { get; set; }

            public void Dispatch()
            {
                if (Coms == null || String.IsNullOrEmpty(Message))
                    return;

                Coms.SendText("%1" + Message + "\x0D");
            }

            public override string ToString()
            {
                return Message;
            }
        }

        public const string SourceComputer = "SOURCE 11";
        public const string SourceHdmi = "SOURCE 30";
        public const string SourceVideo = "SOURCE 45";
        public const string SourceDvi = "SOURCE A0";
        public const string MuteOn = "MUTE ON";
        public const string MuteOff = "MUTE OFF";
        public const string FreezeOn = "FREEZE ON";
        public const string FreezeOff = "FREEZE OFF";
        public const string PowerOn = "POWR 1";
        public const string PowerOff = "POWR 0";
        public const string PowerPoll = "POWR ?";
        public const string SourcePoll = "SOURCE?";
        public const string LampPoll = "LAMP?";
        public const string MutePoll = "MUTE?";
        public const string FreezePoll = "FREEZE?";
        public const string SerialNumberPoll = "SNO?";
		public const string FocusInc = "FOCUS INC";
		public const string FocusDec = "FOCUS DEC";
		public const string ZoomInc = "ZOOM INC";
		public const string ZoomDec = "ZOOM DEC";
		public const string HLensInc = "HLENS INC 10";
		public const string HLensDec = "HLENS DEC 10";
		public const string VLensInc = "LENS INC 10";
		public const string VLensDec = "LENS DEC 10";
    }
}