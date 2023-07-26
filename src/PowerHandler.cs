using System;
using PepperDash.Core;

namespace EpsonProjectorEpi
{
    public class PowerHandler : IKeyed
    {
        public const string SearchString = "PWR=";
        public const string PowerOffResponse = "PWR=00";
        public const string PowerOnResponse = "PWR=01";
        public const string WarmingResponse = "PWR=02";
        public const string CoolingResponse = "PWR=03";
        public const string StandbyResponse = "PWR=04";
        public const string AbnormalStandbyResponse = "PWR=05";

        public enum PowerStatusEnum
        {
            PowerOn = 1, PowerWarming = 2, PowerCooling = 3, PowerOff = 4, None = 0
        }

        public event EventHandler<Events.PowerEventArgs> PowerStatusUpdated;

        public PowerHandler(string key)
        {
            Key = key;
        }

        public void ProcessResponse(string response)
        {
            if (!response.Contains(SearchString))
                return;

            if (response.Contains(PowerOffResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                    {
                        Status = PowerStatusEnum.PowerOff,
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

            if (response.Contains(StandbyResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                {
                    Status = PowerStatusEnum.PowerOff,
                });

                return;
            }

            if (response.Contains(AbnormalStandbyResponse))
            {
                OnPowerUpdated(new Events.PowerEventArgs
                {
                    Status = PowerStatusEnum.PowerOff,
                });

                Debug.Console(1, this, Debug.ErrorLogLevel.Warning, "Received abnormal power status");
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

            Debug.Console(1, this, Debug.ErrorLogLevel.Notice, "Received an unknown power response:{0}", response);
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