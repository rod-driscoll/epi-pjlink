using PepperDash.Core;
using Serilog.Events;
using System;
using System.Text.RegularExpressions;

namespace PJLinkProjectorEpi
{
    public class ClassHandler : IKeyed
    {
        public const string SearchString = Commands.Class + "="; // "CLSS="

        public string Class { private get; set; }

        public event EventHandler<Events.StringEventArgs> ClassUpdated;

        public ClassHandler(string key)
        {
            Key = key;
            Class = "%1"; // default
        }

        public void ProcessResponse(string response)
        {
            Debug.LogMessage(LogEventLevel.Verbose, this, "Response: {0}", response);

            if (!response.Contains(SearchString)) //"CLSS=1"
                return;

            Match result = Regex.Match(response, SearchString + @"(\d+)"); //@"CLSS=(\d+)"
            if (result.Success)
            {
                Class = "%" + result.Groups[1].Value;
                OnClassUpdated(new Events.StringEventArgs
                {
                    Val = result.Groups[1].Value,
                });
                return;
            }

            Debug.LogMessage(LogEventLevel.Debug, this, "Received an unknown auth response:{0}", response);
        }

        private void OnClassUpdated(Events.StringEventArgs args)
        {
            var handler = ClassUpdated;
            if (handler == null)
                return;

            if (args == null)
                Debug.LogMessage(LogEventLevel.Debug, this, "OnClassUpdated args == null");
            else if (args.Val == null)
                Debug.LogMessage(LogEventLevel.Debug, this, "OnClassUpdated Val == null");
            else
                Debug.LogMessage(LogEventLevel.Debug, this, "OnClassUpdated({0})", args.Val);

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}