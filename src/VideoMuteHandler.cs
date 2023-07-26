using System;
using PepperDash.Core;

namespace EpsonProjectorEpi
{
    public class VideoMuteHandler : IKeyed
    {
        public const string SearchString = "MUTE=";
        public const string VideoMuteOffResponse = "MUTE=OFF";
        public const string VideoMuteOnResponse = "MUTE=ON";

        public enum VideoMuteStatusEnum
        {
            Muted = 1, Unmuted = 2, None = 0
        }

        public event EventHandler<Events.VideoMuteEventArgs> VideoMuteStatusUpdated;

        public VideoMuteHandler(string key)
        {
            Key = key;
        }

        public void ProcessResponse(string response)
        {
            if (!response.Contains(SearchString))
                return;

            if (response.Contains(VideoMuteOffResponse))
            {
                OnMuteUpdated(new Events.VideoMuteEventArgs
                    {
                        Status = VideoMuteStatusEnum.Unmuted,
                    });

                return;
            }

            if (response.Contains(VideoMuteOnResponse))
            {
                OnMuteUpdated(new Events.VideoMuteEventArgs
                    {
                        Status = VideoMuteStatusEnum.Muted,
                    });

                return;
            }

            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Received an unknown mute response:{0}", response);
        }

        private void OnMuteUpdated(Events.VideoMuteEventArgs args)
        {
            var handler = VideoMuteStatusUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}