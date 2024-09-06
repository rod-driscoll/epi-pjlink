using System;
using PepperDash.Core;
using System.Text.RegularExpressions;
using Serilog.Events;

namespace PJLinkProjectorEpi
{
    public class PowerHandler : IKeyed
    {
        public const string SearchString = Commands.Power + "="; // "POWR="
        //public const string PowerOffResponse = SearchString + Commands.Standby;  // "%1POWR=0"
        public const string PowerOnResponse = SearchString + Commands.On;
        public const string WarmingResponse = SearchString + Commands.Warming;
        public const string CoolingResponse = SearchString + Commands.Cooling;
        public const string StandbyResponse = SearchString + Commands.Off;
        public const string AckResponse = SearchString + Commands.Ack;

        public const string ErrorResponse = SearchString + Commands.Err; // "%1POWR=ERR"

        public enum PowerStatusEnum
        {
            None = 0, PowerOn = 1, PowerWarming = 2, PowerCooling = 3, PowerStandby = 4, Error = 5 
        }

        public event EventHandler<Events.PowerEventArgs> PowerStatusUpdated;

        public PowerHandler(string key)
        {
            Key = key;
        }

        public void ProcessResponse(string response)
        {
            if (!response.Contains(SearchString)) //"%1POWR="
                return;

            if (response.Contains(StandbyResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                    {
                        Status = PowerStatusEnum.PowerStandby,
                    });

                return;
            }

            if (response.Contains(PowerOnResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                {
                    Status = PowerStatusEnum.PowerOn,
                });

                return;
            }

            if (response.Contains(ErrorResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                {
                    Status = PowerStatusEnum.Error,
                });


                Match result = Regex.Match(response, ErrorResponse + @"(\d)"); //@"%1POWR=ERR(\d)"
                if (result.Success)
                {
                    var msg_ = String.Format("Received power status error: '{0}'", result.Groups[1].Value);
                    if(Commands.ErrorMessage.ContainsKey(result.Groups[1].Value))
                        msg_ = msg_ + ": " + Commands.ErrorMessage[result.Groups[1].Value];
                    Debug.LogMessage(LogEventLevel.Debug, this, msg_);
                }
                else
                    Debug.LogMessage(LogEventLevel.Debug, this, "Received power status ERROR: '{0}'", ErrorResponse);
                return;
            }

            if (response.Contains(WarmingResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                {
                    Status = PowerStatusEnum.PowerWarming,
                });

                return;
            }

            if (response.Contains(CoolingResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                {
                    Status = PowerStatusEnum.PowerCooling,
                });

                return;
            }

            if (response.Contains(AckResponse))
            {
                return;
            }

            Debug.LogMessage(LogEventLevel.Warning, this, "Received an unknown power response:{0}", response);
            Debug.LogMessage(LogEventLevel.Debug, this, "AckResponse: {0}", AckResponse);
        }

        private void OnPowerUpdated(Events.PowerEventArgs args)
        {
            var handler = PowerStatusUpdated;
            if (handler == null)
                return;

            handler.Invoke(this, args);
        }

        public string Key { get; private set; }
    }
}