using System;
using Crestron.SimplSharp;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Queues;

namespace EpsonProjectorEpi
{
    public class LampHoursHandler : IKeyed
    {
        public string Key { get; private set; }
        private readonly GenericQueue _queue;
        private readonly CommunicationGather _gather;
        private CTimer _pollTimer;
        private int _lampHours;

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
            var result = genericCommMethodReceiveTextArgs.Text;
            if (!result.Contains("LAMP="))
                return;

            var index = result.IndexOf("=", StringComparison.Ordinal) + 1;
            _lampHours = Convert.ToInt32(result.Remove(0, index));
            LampHoursFeedback.FireUpdate();
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
                Message = Commands.LampPoll,
            }), null, 16548);
        }

        public IntFeedback LampHoursFeedback { get; private set; }
    }
}