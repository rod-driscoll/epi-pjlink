using System;
using PepperDash.Core;

namespace EpsonProjectorEpi
{
    public class VideoFreezeHandler : IKeyed
    {
        public const string SearchString = "FREEZE=";
        public const string VideoFreezeOffResponse = "FREEZE=OFF";
        public const string VideoFreezeOnResponse = "FREEZE=ON";

        public enum VideoFreezeStatusEnum
        {
            Frozen = 1, Unfrozen = 2, None = 0
        }

        public event EventHandler<Events.VideoFreezeEventArgs> VideoFreezeStatusUpdated;

        public VideoFreezeHandler(string key)
        {
            Key = key;
        }

        public void ProcessResponse(string response)
        {
            if (!response.Contains(SearchString))
                return;

            if (response.Contains(VideoFreezeOffResponse))
            {
                OnFreezeUpdated(new Events.VideoFreezeEventArgs
                {
                    Status = VideoFreezeStatusEnum.Unfrozen,
                });

                return;
            }

            if (response.Contains(VideoFreezeOnResponse))
            {
                OnFreezeUpdated(new Events.VideoFreezeEventArgs
                {
                    Status = VideoFreezeStatusEnum.Frozen,
                });

                return;
            }

            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Received an unknown freeze response:{0}", response);
        }

        private void OnFreezeUpdated(Events.VideoFreezeEventArgs args)
        {
            var handler = VideoFreezeStatusUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}