using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace EpsonProjectorEpi
{
    public class SerialNumberHandler : IKeyed
    {
        public string Key { get; private set; }
        private readonly GenericQueue _queue;
        private readonly CommunicationGather _gather;
        private CTimer _pollTimer;
        private string _serialNumber;

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
            if (!result.Contains("SNO="))
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

            _pollTimer = new CTimer(o => _queue.Enqueue(new Commands.EpsonCommand
                {
                    Coms = _gather.Port as IBasicCommunication,
                    Message = Commands.SerialNumberPoll,
                }), null, 23564);
        }

        public StringFeedback SerialNumberFeedback { get; private set; }
    }
}