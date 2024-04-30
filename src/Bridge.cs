using System;
using System.Linq;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;

namespace PJLinkProjectorEpi
{
    public class Bridge
    {
        public static void LinkToApi(PJLinkProjector proj, BasicTriList trilist, JoinMap joinMap)
        {
            SetupNameFb(proj, trilist, joinMap);

            var isProjFb = new BoolFeedback(() => true);
            isProjFb.LinkInputSig(trilist.BooleanInput[joinMap.IsProjector.JoinNumber]);
            isProjFb.FireUpdate();

            trilist.SetSigTrueAction(joinMap.PowerOn.JoinNumber, proj.PowerOn);
            trilist.SetSigTrueAction(joinMap.PowerOff.JoinNumber, proj.PowerOff);
            trilist.SetSigTrueAction(joinMap.MuteOn.JoinNumber, proj.VideoMuteOn);
            trilist.SetSigTrueAction(joinMap.MuteOff.JoinNumber, proj.VideoMuteOff);
            trilist.SetSigTrueAction(joinMap.MuteToggle.JoinNumber, proj.VideoMuteToggle);
            trilist.SetSigTrueAction(joinMap.FreezeOn.JoinNumber, proj.VideoFreezeOn);
            trilist.SetSigTrueAction(joinMap.FreezeOff.JoinNumber, proj.VideoFreezeOff);
            trilist.SetSigTrueAction(joinMap.FreezeToggle.JoinNumber, proj.VideoFreezeToggle);
            trilist.SetUShortSigAction(joinMap.InputSelectOffset.JoinNumber,
                value =>
                    {
                        if (value == 0)
                            proj.VideoMuteOn();
                        else
                        {
                            proj.ExecuteSwitch(value);
                        }
                    });

            for (var x = 0; x < joinMap.InputSelectOffset.JoinNumber; x++)
                LinkInputSelect(proj, trilist, joinMap, x);

            proj.PowerIsOnFeedback.LinkInputSig(trilist.BooleanInput[joinMap.PowerOn.JoinNumber]);
            proj.IsWarmingUpFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Warming.JoinNumber]);
            proj.IsCoolingDownFeedback.LinkInputSig(trilist.BooleanInput[joinMap.Cooling.JoinNumber]);
            proj.VideoMuteIsOn.LinkInputSig(trilist.BooleanInput[joinMap.MuteOn.JoinNumber]);
            proj.VideoMuteIsOff.LinkInputSig(trilist.BooleanInput[joinMap.MuteOff.JoinNumber]);
            proj.VideoFreezeIsOn.LinkInputSig(trilist.BooleanInput[joinMap.FreezeOn.JoinNumber]);
            proj.VideoFreezeIsOff.LinkInputSig(trilist.BooleanInput[joinMap.FreezeOff.JoinNumber]);
            proj.IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
            proj.LampHoursFeedback.LinkInputSig(trilist.UShortInput[joinMap.LampHours.JoinNumber]);
            proj.CurrentInputValueFeedback.LinkInputSig(trilist.UShortInput[joinMap.InputSelectOffset.JoinNumber]);
            proj.SerialNumberFeedback.LinkInputSig(trilist.StringInput[joinMap.SerialNumber.JoinNumber]);
			trilist.SetUShortSigAction(joinMap.LensPositionMemory.JoinNumber, (i) => proj.LensPositionRecall(i));
			trilist.SetSigHeldAction(joinMap.HShiftPlus.JoinNumber, 250, 
				() => proj.StartLensMoveRepeat(eLensFunction.HShiftPlus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.HShiftPlus)
			);
			trilist.SetSigHeldAction(joinMap.HShiftMinus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.HShiftMinus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.HShiftMinus)
			);
			trilist.SetSigHeldAction(joinMap.VShiftPlus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.VShiftPlus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.VShiftPlus)
			);
			trilist.SetSigHeldAction(joinMap.VShiftMinus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.VShiftMinus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.VShiftMinus)
			);
			trilist.SetSigHeldAction(joinMap.ZoomPlus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.ZoomPlus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.ZoomPlus)
			);
			trilist.SetSigHeldAction(joinMap.ZoomMinus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.ZoomMinus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.ZoomMinus)
			);
			trilist.SetSigHeldAction(joinMap.FocusPlus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.FocusPlus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.FocusPlus)
			);
			trilist.SetSigHeldAction(joinMap.FocusMinus.JoinNumber, 250,
				() => proj.StartLensMoveRepeat(eLensFunction.FocusMinus),
				() => proj.StopLensMoveRepeat(),
				() => proj.LensFunction(eLensFunction.FocusMinus)
			);
        }

        private static void LinkInputSelect(IRoutingSinkWithSwitching proj, BasicTriList trilist, JoinMap joinMap, int x)
        {
            var joinActual = joinMap.InputSelectOffset.JoinNumber + x;
            var inputActual = x + 1;

            var routingPort =
                proj.InputPorts.Where(port => port.Port != null)
                    .FirstOrDefault(t => Convert.ToInt32(t.Port) == inputActual);

            if (routingPort == null) return;

            Debug.Console(1, proj, "Linking input:{0} to join {1}", routingPort.Key, joinActual);

            trilist.SetSigTrueAction((uint) joinActual, () => proj.ExecuteSwitch(routingPort.Selector));

            var fb = new StringFeedback(() => routingPort.Key);
            fb.LinkInputSig(trilist.StringInput[(uint) joinActual]);
            fb.FireUpdate();
        }

        private static void SetupNameFb(IKeyName proj, BasicTriList trilist, JoinMap joinMap)
        {
            var nameFb = new StringFeedback(() => proj.Name);
            nameFb.LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber]);
            nameFb.FireUpdate();
        }
    }
}