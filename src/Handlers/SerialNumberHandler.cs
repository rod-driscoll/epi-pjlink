using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace PJLinkProjectorEpi
{
    public class SerialNumberHandler : IKeyed, IHasCommandAuthString
    {        
        public string Key { get; private set; }
        private readonly GenericQueue _queue;
        private readonly CommunicationGather _gather;
        private CTimer _pollTimer;
        private string _serialNumber;
        private string _authString;
        public string AuthString
        {
            get { return String.IsNullOrEmpty(_authString) ? String.Empty : _authString; }
            set { _authString = value; }
        }        
        private string _classString;
        public string ClassString
        {
            get { return String.IsNullOrEmpty(_classString) ? String.Empty : _classString; }
            set { _classString = value; }
        }

        public SerialNumberHandler(string key, GenericQueue queue, CommunicationGather gather, Feedback powerIsOn)
        {
            Key = key;
            _queue = queue;
            _gather = gather;
            _gather.LineReceived += HandleLineReceived;
            powerIsOn.OutputChange += (sender, args) =>
                {
                    if (args.BoolValue)
                        Poll();
                };

            SerialNumberFeedback = new StringFeedback("SerialNumber", () => _serialNumber);
        }

        private void HandleLineReceived(object sender, GenericCommMethodReceiveTextArgs genericCommMethodReceiveTextArgs)
        {
            var result = genericCommMethodReceiveTextArgs.Text;
            if (!result.Contains(Commands.SerialNumber + "=")) // "%1SNUM="
                return;

            var index = result.IndexOf("=", StringComparison.Ordinal) + 1;
            _serialNumber = result.Remove(0, index).TrimEnd('\x0D');
            SerialNumberFeedback.FireUpdate();
        }

        private void Poll()
        {
            if (_pollTimer != null)
            {
                _pollTimer.Stop();
                _pollTimer.Dispose();
            }

            _pollTimer = new CTimer(o => 
                {
                    if(String.Equals(ClassString, Commands.Protocol2))
                    {
                        _queue.Enqueue(new Commands.PJLinkCommand
                        {
                            Coms = _gather.Port as IBasicCommunication,
                            Message = AuthString + Commands.Protocol2 + Commands.SerialNumber + Commands.Query, // "SNUM ?"
                        });
                    }
                }
                , null, 23564);
        }

        public StringFeedback SerialNumberFeedback { get; private set; }
    }
}