using System;
using PepperDash.Core;
using System.Text.RegularExpressions;

namespace PJLinkProjectorEpi
{
    public class VideoInputHandler : IKeyed
    {
        public const string SearchString = Commands.Protocol1 + Commands.Source + "="; // "%1INPT="
        public const string ErrorResponse = SearchString + Commands.Err; // "%1INPT=ERR"

        public enum VideoInputStatusEnum
        {
            None = 0, RGB = 1, Video = 2, Digital = 3, Storage = 4, Network = 5
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

            Match result = Regex.Match(response, SearchString + @"(\d+)"); //@"%1INPT=(\d+)"
            if (result.Success)
            {
                OnMuteUpdated(new Events.VideoInputEventArgs
                {
                    Input = Convert.ToUInt32(result.Groups[1].Value),
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