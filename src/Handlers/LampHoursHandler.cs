using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;
using System.Text.RegularExpressions;

namespace PJLinkProjectorEpi
{
    public class LampHoursHandler : IKeyed, IHasCommandAuthString
    {
        public string Key { get; private set; }
        private readonly GenericQueue _queue;
        private readonly CommunicationGather _gather;
        private CTimer _pollTimer;
        private int _lampHours;
        private string _authString;
        public string AuthString
        {
            get { return String.IsNullOrEmpty(_authString) ? String.Empty : _authString; }
            set { _authString = value; }
        }
        public string ClassString { get; set; }

        public LampHoursHandler(string key, GenericQueue queue, CommunicationGather gather, Feedback powerIsOn)
        {
            Key = key;
            _queue = queue;
            _gather = gather;
            _gather.LineReceived += HandleLineReceived;
            LampHoursFeedback = new IntFeedback("LampHours", () => _lampHours);
            powerIsOn.OutputChange += (sender, args) =>
                {
                    if (args.BoolValue)
                        Poll();
                };
        }

        private void HandleLineReceived(object sender, GenericCommMethodReceiveTextArgs genericCommMethodReceiveTextArgs)
        {
            try
            {
                var response = genericCommMethodReceiveTextArgs.Text;
                if (!response.Contains(Commands.LampUsage + "=")) // "LAMP="
                    return;

                var ErrorResponse = Commands.LampUsage + "=" + Commands.Err;
                if (response.Contains(ErrorResponse))
                {
                    Match result = Regex.Match(response, ErrorResponse + @"(\d)"); //@"LAMP=ERR(\d)"
                    if (result.Success)
                    {
                        var msg_ = String.Format("Received lamp status NOTICE: '{0}'", result.Groups[1].Value);

                        if (result.Groups[1].Value == "1")
                        {
                            msg_ = msg_ + ": 'NO LAMP'";
                            Debug.Console(1, this, msg_);
                        }
                        else if (Commands.ErrorMessage.ContainsKey(result.Groups[1].Value))
                        {
                            msg_ = msg_ + ": " + Commands.ErrorMessage[result.Groups[1].Value];
                            Debug.Console(1, this, Debug.ErrorLogLevel.Warning, msg_);
                        }
                    }
                    else
                        Debug.Console(1, this, Debug.ErrorLogLevel.Warning, "Received power status ERROR: '{0}'", ErrorResponse);
                    return;
                }

                var index = response.IndexOf("=", StringComparison.Ordinal) + 1;
                //Debug.Console(1, this, "HandleLineReceived index: '{0}', string: {1}", index, result);
                //_lampHours = Convert.ToInt32(result.Remove(0, index));
                var s = response.Remove(0, index);
                //Debug.Console(1, this, "HandleLineReceived new: '{0}'", s);
                _lampHours = Convert.ToInt32(s);
                Debug.Console(1, this, "HandleLineReceived _lampHours: '{0}'", _lampHours);
                LampHoursFeedback.FireUpdate();
            }
            catch (Exception e)
            {
                Debug.Console(1, this, Debug.ErrorLogLevel.Error, "HandleLineReceived ERROR: '{0}'", e.Message);
            }
        }

        private void Poll()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
            }

            _pollTimer = new CTimer(o => _queue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _gather.Port as IBasicCommunication,
                Message = AuthString + Commands.Protocol1 + Commands.LampUsage + Commands.Query,
            }), null, 16548);
        }

        public IntFeedback LampHoursFeedback { get; private set; }

    }
}