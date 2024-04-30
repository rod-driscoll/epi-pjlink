using System;

namespace PJLinkProjectorEpi
{
    public static class Events
    {
        public class PowerEventArgs : EventArgs
        {
            public PowerHandler.PowerStatusEnum Status { get; set; }
        }

        public class VideoMuteEventArgs : EventArgs
        {
            public VideoMuteHandler.VideoMuteStatusEnum Status { get; set; }
        }

        public class VideoFreezeEventArgs : EventArgs
        {
            public VideoFreezeHandler.VideoFreezeStatusEnum Status { get; set; }
        }

        public class VideoInputEventArgs : EventArgs
        {
            public uint Input { get; set; }
        }

        public class AuthEventArgs : EventArgs
        {
            public string MD5 { get; set; }
        }
    }
}