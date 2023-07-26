using System;

namespace EpsonProjectorEpi
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
            public VideoInputHandler.VideoInputStatusEnum Input { get; set; }
        }
    }
}