using System;
using PepperDash.Core;

namespace EpsonProjectorEpi
{
    public class VideoInputHandler : IKeyed
    {
        public const string SearchString = "SOURCE=";
        public const string VideoInputHdmi = "SOURCE=30";
        public const string VideoInputDvi = "SOURCE=A0";
        public const string VideoInputComputer = "SOURCE=11";
        public const string VideoInputVideo = "SOURCE=45";

        public enum VideoInputStatusEnum
        {
            None = 0, Hdmi = 1, Dvi = 2, Computer = 3, Video = 4 
        }

        public event EventHandler<Events.VideoInputEventArgs> VideoInputUpdated;

        public VideoInputHandler(string key)
        {
            Key = key;
        }

        public void ProcessResponse(string response)
        {
            if (!response.Contains(SearchString))
                return;

            if (response.Contains(VideoInputHdmi))
            {
                OnMuteUpdated(new Events.VideoInputEventArgs
                    {
                        Input = VideoInputStatusEnum.Hdmi,
                    });

                return;
            }

            if (response.Contains(VideoInputDvi))
            {
                OnMuteUpdated(new Events.VideoInputEventArgs
                    {
                        Input = VideoInputStatusEnum.Dvi,
                    });

                return;
            }

            if (response.Contains(VideoInputComputer))
            {
                OnMuteUpdated(new Events.VideoInputEventArgs
                    {
                        Input = VideoInputStatusEnum.Computer,
                    });

                return;
            }

            if (response.Contains(VideoInputVideo))
            {
                OnMuteUpdated(new Events.VideoInputEventArgs
                    {
                        Input = VideoInputStatusEnum.Video,
                    });

                return;
            }

            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Received an unknown video input response:{0}", response);
        }

        private void OnMuteUpdated(Events.VideoInputEventArgs args)
        {
            var handler = VideoInputUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}