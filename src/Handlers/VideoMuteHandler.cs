using System;
using PepperDash.Core;

namespace PJLinkProjectorEpi
{
    public class VideoMuteHandler : IKeyed
    {
        public const string SearchString = Commands.Mute + "="; // "%1AVMT="
        public const string VideoMuteOffResponse= SearchString + Commands.Video + Commands.Off; //"AVMT=10"
        public const string VideoMuteOnResponse = SearchString + Commands.Video + Commands.On;
        public const string AudioMuteOffResponse= SearchString + Commands.Audio + Commands.Off;
        public const string AudioMuteOnResponse = SearchString + Commands.AV + Commands.On;
        public const string AVMuteOffResponse   = SearchString + Commands.AV + Commands.Off;
        public const string AVMuteOnResponse = SearchString + Commands.AV + Commands.On;

        public enum VideoMuteStatusEnum
        {
            Muted = 1, Unmuted = 2, None = 0
        }

        public event EventHandler<Events.VideoMuteEventArgs> VideoMuteStatusUpdated;
        public event EventHandler<Events.VideoMuteEventArgs> AudioMuteStatusUpdated;

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
                OnVideoMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Unmuted,
                });
                return;
            }
            if (response.Contains(VideoMuteOnResponse))
            {
                OnVideoMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Muted,
                });
                return;
            }
            if (response.Contains(AVMuteOffResponse))
            {
                OnVideoMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Unmuted,
                });
                OnAudioMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Unmuted,
                });
                return;
            }
            if (response.Contains(AVMuteOnResponse))
            {
                OnVideoMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Muted,
                });
                OnAudioMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Muted,
                });

                return;
            }
            if (response.Contains(AudioMuteOffResponse))
            {
                OnAudioMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Unmuted,
                });
                return;
            }
            if (response.Contains(AudioMuteOnResponse))
            {
                OnAudioMuteUpdated(new Events.VideoMuteEventArgs
                {
                    Status = VideoMuteStatusEnum.Muted,
                });
                return;
            }

            Debug.Console(1, this, "Received an unknown mute response:{0}", response);
        }

        private void OnVideoMuteUpdated(Events.VideoMuteEventArgs args)
        {
            var handler = VideoMuteStatusUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }
        private void OnAudioMuteUpdated(Events.VideoMuteEventArgs args)
        {
            var handler = AudioMuteStatusUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}