using System.Linq;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace EpsonProjectorEpi
{
    public static class FeedbackExt
    {
        public static void FireAllFeedbacks(this FeedbackCollection<Feedback> feedbacks)
        {
            foreach (var feedback in feedbacks.Where(x => x != null))
                feedback.FireUpdate();
        }

        public static void RegisterForConsoleUpdates(this FeedbackCollection<Feedback> feedbacks, IKeyed keyed)
        {
            foreach (var item in feedbacks.Where(x => x != null && !string.IsNullOrEmpty(x.Key)))
            {
                var feedback = item;
                if (feedback is StringFeedback)
                    feedback.OutputChange +=
                        (sender, args) =>
                            Debug.Console(1,
                                keyed,
                                "Received an update {0}: '{1}'",
                                feedback.Key,
                                feedback.StringValue);

                if (feedback is IntFeedback)
                    feedback.OutputChange +=
                        (sender, args) =>
                            Debug.Console(1,
                                keyed,
                                "Received an update {0}: '{1}'",
                                feedback.Key,
                                feedback.IntValue);

                if (feedback is BoolFeedback)
                    feedback.OutputChange +=
                        (sender, args) =>
                            Debug.Console(1,
                                keyed,
                                "Received an update {0}: '{1}'",
                                feedback.Key,
                                feedback.BoolValue);
            }
        }
    }
}