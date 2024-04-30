using System;
using PepperDash.Core;
using PepperDash.Essentials.Core.Queues;
using System.Collections.Generic;

namespace PJLinkProjectorEpi
{
    public static class Commands
    {
        public class PJLinkCommand : IQueueMessage
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

        public static Dictionary<string, string> ErrorMessage = new Dictionary<string, string>
        {
            { Commands.ErrNotSupported, "Not supported" },
            { Commands.ErrParameter, "Out of parameter" },
            { Commands.ErrUnavailable, "Unavailable time" },
            { Commands.ErrDisplay, "Display failure" }
        };

        //public List<string> AllQueryCommands = new List<string>{
        //    Power, Source, InputNames, SourceList, Mute, Volume, MicVolume, Freeze,
        //    LampUsage, LampModel, FilterUsage, FilterModel, Name, Info, Manufacturer,
        //    Model, Class, SerialNumber, SoftwareVersion, InputResolution, RecomendedResolution
        //};

        public const string AuthNotice = "PJLINK";
        public const string AuthError = "ERRA"; // "PJLINK ERRA"
        public const string ErrorStatus = "ERST";
        public const string Protocol1 = "%1";
        public const string Query = " ?";


        public const string Power = "POWR"; // e.g. "POWR ?", "POWR 1", "POWR="
        public const string Off = "0";
        public const string On = "1";
        public const string Cooling = "2";
        public const string Warming = "3";

        public const string Err = "ERR";
        public const string ErrNotSupported = "1";
        public const string ErrParameter = "2";
        public const string ErrUnavailable = "3";
        public const string ErrDisplay = "4";

        public const string Source = "INPT";
        public const string InputNames = "INNM";
        public const string SourceList = "INST";

        public const uint SourceOffsetRGB = 10; // "INPT 11"
        public const uint SourceOffsetVideo = 20;
        public const uint SourceOffsetDigital = 30;
        public const uint SourceOffsetStorage = 40;
        public const uint SourceOffsetNetwork = 50;

        public const string Mute = "AVMT"; // "AVMT ?"
        public const string MuteVideo = "AVMT 1"; // "AVMT 10"
        public const string MuteAudio = "AVMT 2";
        public const string MuteAV = "AVMT 3";
        public const string Volume = "SVOL";
        public const string MicVolume = "MVOL";

        public const string Freeze = "FREZ";

        public const string LampUsage = "LAMP"; // "LAMP ?"
        public const string LampModel = "RLMP";
        public const string FilterUsage = "FILT";
        public const string FilterModel = "RFIL";
        public const string Name = "NAME";
        public const string Info = "INFO";
        public const string Manufacturer = "INF1";
        public const string Model = "INF2";
        public const string Class = "CLSS";
        public const string SerialNumber = "SNUM";
        public const string SoftwareVersion = "SVER";
        public const string InputResolution = "IRES";
        public const string RecomendedResolution = "RRES";
    }
}