using System;

using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Queues;
using Feedback = PepperDash.Essentials.Core.Feedback;
using Thread = Crestron.SimplSharpPro.CrestronThread.Thread;
using System.Collections.Generic;

namespace PJLinkProjectorEpi
{
    public interface IHasCommandPrefix
    {
        string Prefix { get; set; }
    }

    public class PJLinkProjector : EssentialsBridgeableDevice, IRoutingSinkWithSwitching, IHasPowerControlWithFeedback,
        IWarmingCooling, IOnline, IBasicVideoMuteWithFeedback, ICommunicationMonitor, IHasFeedback
    {
        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _commandQueue;
        private CTimer _pollTimer;
		private CTimer _LensTimer; 
        private const int _pollTime = 6000;

        private PowerHandler.PowerStatusEnum _currentPowerStatus;
        private PowerHandler.PowerStatusEnum _requestedPowerStatus;

        private VideoMuteHandler.VideoMuteStatusEnum _currentVideoMuteStatus;
        private VideoMuteHandler.VideoMuteStatusEnum _requestedMuteStatus;

        private VideoFreezeHandler.VideoFreezeStatusEnum _currentVideoFreezeStatus;
        private VideoFreezeHandler.VideoFreezeStatusEnum _requestedFreezeStatus;

        private uint _currentVideoInput;
        private uint _requestedVideoInput;

        private string Prefix = String.Empty;
        private List<IHasCommandPrefix> DriversRequiringPrefix = new List<IHasCommandPrefix>();

        public PJLinkProjector(string key, string name, PropsConfig config, IBasicCommunication coms) : base(key, name)
        {
            _coms = coms;
            if (config.Monitor == null)
                config.Monitor = GetDefaultMonitorConfig();

            CommunicationMonitor = new GenericCommunicationMonitor(this, coms, config.Monitor);
            var gather = new CommunicationGather(coms, "\x0D");

            _commandQueue = new GenericQueue(key + "-command-queue", 213, Thread.eThreadPriority.MediumPriority, 50);

            InputPorts = new RoutingPortCollection<RoutingInputPort>();
            for (uint i = 1; i < 10; i++)
            {
                string key_ = "RGB " + i.ToString();
                InputPorts.Add(new RoutingInputPort(key_,
                        eRoutingSignalType.Video,
                        eRoutingPortConnectionType.Rgb,
                        VideoInputHandler.VideoInputStatusEnum.RGB,
                        this) { Port = Commands.SourceOffsetRGB + i });
                key_ = "Video " + i.ToString();
                InputPorts.Add(new RoutingInputPort(key_,
                        eRoutingSignalType.Video,
                        eRoutingPortConnectionType.Composite,
                        VideoInputHandler.VideoInputStatusEnum.Video,
                        this) { Port = Commands.SourceOffsetVideo + i });
                key_ = "Digital " + i.ToString();
                InputPorts.Add(new RoutingInputPort(key_,
                        eRoutingSignalType.Video,
                        eRoutingPortConnectionType.Hdmi,
                        VideoInputHandler.VideoInputStatusEnum.Digital,
                        this) { Port = Commands.SourceOffsetDigital + i });
                key_ = "Storage " + i.ToString();
                InputPorts.Add(new RoutingInputPort(key_,
                        eRoutingSignalType.Video,
                        eRoutingPortConnectionType.Streaming,
                        VideoInputHandler.VideoInputStatusEnum.Storage,
                        this) { Port = Commands.SourceOffsetStorage + i });
                key_ = "Network " + i.ToString();
                InputPorts.Add(new RoutingInputPort(key_,
                        eRoutingSignalType.Video,
                        eRoutingPortConnectionType.Composite,
                        VideoInputHandler.VideoInputStatusEnum.Network,
                        this) { Port = Commands.SourceOffsetNetwork + i });
            }

            PowerIsOnFeedback =
                new BoolFeedback("PowerIsOn", () => _currentPowerStatus == PowerHandler.PowerStatusEnum.PowerOn);

            IsWarmingUpFeedback =
                new BoolFeedback("IsWarmingUp", () => _currentPowerStatus == PowerHandler.PowerStatusEnum.PowerWarming);

            IsCoolingDownFeedback =
                new BoolFeedback("IsCoolingDown", () => _currentPowerStatus == PowerHandler.PowerStatusEnum.PowerCooling);

            PowerIsOffFeedback =
                new BoolFeedback(() => _currentPowerStatus == PowerHandler.PowerStatusEnum.PowerStandby);

            VideoMuteIsOn =
                new BoolFeedback("VideoMuteIsOn", () => _currentVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Muted && PowerIsOnFeedback.BoolValue);

            VideoMuteIsOff =
                new BoolFeedback(() => !VideoMuteIsOn.BoolValue && PowerIsOnFeedback.BoolValue);

            VideoFreezeIsOn =
                new BoolFeedback("VideoFreezeIsOn", () => _currentVideoFreezeStatus == VideoFreezeHandler.VideoFreezeStatusEnum.Frozen && PowerIsOnFeedback.BoolValue);

            VideoFreezeIsOff =
                new BoolFeedback(() => !VideoFreezeIsOn.BoolValue && PowerIsOnFeedback.BoolValue);

            var powerHandler = new PowerHandler(key);
            powerHandler.PowerStatusUpdated += HandlePowerStatusUpdated;

            var muteHandler = new VideoMuteHandler(key);
            muteHandler.VideoMuteStatusUpdated += HandleMuteStatusUpdated;

            var freezeHandler = new VideoFreezeHandler(key);
            freezeHandler.VideoFreezeStatusUpdated += HandleFreezeStatusUpdated;

            var inputHandler = new VideoInputHandler(key);
            inputHandler.VideoInputUpdated += HandleVideoInputUpdated;

            var authHandler = new AuthHandler(key);
            authHandler.AuthStatusUpdated += HandleAuthStatusUpdated;

            new StringResponseProcessor(gather,
                s =>
                    {
                        authHandler.ProcessResponse(s);
                        powerHandler.ProcessResponse(s);
                        muteHandler.ProcessResponse(s);
                        freezeHandler.ProcessResponse(s);
                        inputHandler.ProcessResponse(s);
                    });

            LampHoursFeedback =
                new LampHoursHandler(key, _commandQueue, gather, PowerIsOnFeedback).LampHoursFeedback;
            DriversRequiringPrefix.Add(LampHoursFeedback as IHasCommandPrefix);

            SerialNumberFeedback =
                new SerialNumberHandler(key, _commandQueue, gather, PowerIsOnFeedback).SerialNumberFeedback;
            DriversRequiringPrefix.Add(SerialNumberFeedback as IHasCommandPrefix);

            CurrentInputValueFeedback =
                new IntFeedback("CurrentInput",
                    () =>
                        {
                            if (!PowerIsOnFeedback.BoolValue)
                                return 0;

                            return (int) _currentVideoInput;
                        });

            Feedbacks = new FeedbackCollection<Feedback>
            {
                PowerIsOnFeedback,
                IsWarmingUpFeedback,
                IsCoolingDownFeedback,
                PowerIsOffFeedback,
                VideoMuteIsOn,
                VideoMuteIsOff,
                VideoFreezeIsOn,
                VideoFreezeIsOff,
                CurrentInputValueFeedback,
                new StringFeedback("RequestedPower", () => _requestedPowerStatus.ToString()),
                new StringFeedback("RequestedMute", () => _requestedMuteStatus.ToString()),
                new StringFeedback("RequestedFreeze", () => _requestedFreezeStatus.ToString()),
                new StringFeedback("RequestedInput", () => _requestedVideoInput.ToString()),
            };

            CrestronEnvironment.ProgramStatusEventHandler += type =>
            {
                if (type != eProgramStatusEventType.Stopping)
                    return;

                if (_pollTimer == null)
                    return;

                _pollTimer.Stop();
                _pollTimer.Dispose();
            };
        }

        private static CommunicationMonitorConfig GetDefaultMonitorConfig()
        {
            return new CommunicationMonitorConfig()
                {
                    PollInterval = 30000,
                    PollString = Commands.Power + " ?\x0D",
                    TimeToWarning = 120000,
                    TimeToError = 360000,
                };
        }

        public override bool CustomActivate()
        {
            Feedbacks.RegisterForConsoleUpdates(this);
            Feedbacks.FireAllFeedbacks();

            _pollTimer = new CTimer(o =>
                {
                    _commandQueue.Enqueue(new Commands.PJLinkCommand
                        {
                            Coms = _coms,
                            Message = Prefix + Commands.Power + Commands.Query
                        });

                    if (!PowerIsOnFeedback.BoolValue)
                        return;

                    _commandQueue.Enqueue(new Commands.PJLinkCommand
                        {
                            Coms = _coms,
                            Message = Prefix + Commands.Source + Commands.Query
                        });

                    _commandQueue.Enqueue(new Commands.PJLinkCommand
                    {
                        Coms = _coms,
                        Message = Prefix + Commands.Mute + Commands.Query
                    });

                    _commandQueue.Enqueue(new Commands.PJLinkCommand
                    {
                        Coms = _coms,
                        Message = Prefix + Commands.Freeze + Commands.Query
                    });
                },
                null,
                5189,
                _pollTime);

            PowerIsOnFeedback.OutputChange += (sender, args) =>
                {
                    if (!args.BoolValue)
                        return;

                    ProcessRequestedVideoInput();
                    ProcessRequestedMuteStatus();
                    ProcessRequestedFreezeStatus();
                };

            CommunicationMonitor.Start();
            return base.CustomActivate();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new JoinMap(joinStart);
            if (bridge != null)
                bridge.AddJoinMap(Key, joinMap);

            Bridge.LinkToApi(this, trilist, joinMap);
        }

        private void HandleAuthStatusUpdated(object sender, Events.AuthEventArgs eventArgs)
        {
            Prefix = eventArgs.MD5;
            if(!String.IsNullOrEmpty(Prefix))
                Prefix = Prefix + Commands.Protocol1;
            foreach(var driver in DriversRequiringPrefix)
                driver.Prefix = Prefix; 
        }

        private void HandlePowerStatusUpdated(object sender, Events.PowerEventArgs eventArgs)
        {
            _currentPowerStatus = eventArgs.Status;
            ProcessRequestedPowerStatus();
            Feedbacks.FireAllFeedbacks();
        }

        private void HandleMuteStatusUpdated(object sender, Events.VideoMuteEventArgs videoMuteEventArgs)
        {
            _currentVideoMuteStatus = videoMuteEventArgs.Status;

            ProcessRequestedMuteStatus();
            Feedbacks.FireAllFeedbacks();
        }

        private void HandleFreezeStatusUpdated(object sender, Events.VideoFreezeEventArgs videoFreezeEventArgs)
        {
            _currentVideoFreezeStatus = videoFreezeEventArgs.Status;

            ProcessRequestedFreezeStatus();
            Feedbacks.FireAllFeedbacks();
        }

        private void ProcessRequestedPowerStatus()
        {
            switch (_requestedPowerStatus)
            {
                case PowerHandler.PowerStatusEnum.PowerOn:
                    ProcessRequestedPowerOn();
                    break;
                case PowerHandler.PowerStatusEnum.PowerWarming:
                    break;
                case PowerHandler.PowerStatusEnum.PowerCooling:
                    break;
                case PowerHandler.PowerStatusEnum.PowerStandby:
                    ProcessRequestedPowerOff();
                    break;
                case PowerHandler.PowerStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessRequestedPowerOn()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn)
                throw new InvalidOperationException("Power on isn't requested");

            switch (_currentPowerStatus)
            {
                case PowerHandler.PowerStatusEnum.PowerOn:
                    _requestedPowerStatus = PowerHandler.PowerStatusEnum.None;
                    break;
                case PowerHandler.PowerStatusEnum.PowerWarming:
                    _requestedPowerStatus = PowerHandler.PowerStatusEnum.None;
                    break;
                case PowerHandler.PowerStatusEnum.PowerCooling:
                    break;
                case PowerHandler.PowerStatusEnum.PowerStandby:
                    _currentPowerStatus = PowerHandler.PowerStatusEnum.PowerWarming;
                    break;

                case PowerHandler.PowerStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = Prefix + Commands.Power + " " + Commands.On,
            });
        }

        private void ProcessRequestedPowerOff()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerStandby)
                throw new InvalidOperationException("Power off isn't requested");
       
            switch (_currentPowerStatus)
            {
                case PowerHandler.PowerStatusEnum.PowerOn:
                    _currentPowerStatus = PowerHandler.PowerStatusEnum.PowerCooling;
                    break;
                case PowerHandler.PowerStatusEnum.PowerWarming:
                    break;
                case PowerHandler.PowerStatusEnum.PowerCooling:
                    _requestedPowerStatus = PowerHandler.PowerStatusEnum.None;
                    break;
                case PowerHandler.PowerStatusEnum.PowerStandby:
                    _requestedPowerStatus = PowerHandler.PowerStatusEnum.None;
                    break;
                case PowerHandler.PowerStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = Prefix + Commands.Power + " " + Commands.Off,
            });
        }

        private void ProcessRequestedMuteStatus()
        {
            if (!PowerIsOnFeedback.BoolValue)
                return;

            switch (_requestedMuteStatus)
            {
                case VideoMuteHandler.VideoMuteStatusEnum.Muted:
                    ProcessRequestedMuteOnStatus();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.Unmuted:
                    ProcessRequestedMuteOffStatus();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessRequestedMuteOnStatus()
        {
            if (_requestedMuteStatus != VideoMuteHandler.VideoMuteStatusEnum.Muted)
                throw new InvalidOperationException("Mute on isn't requested");

            if (_requestedMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.None ||
                _currentVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Muted)
            {
                _requestedMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = Prefix + Commands.Mute + " " + Commands.On,
            });
        }

        private void ProcessRequestedMuteOffStatus()
        {
            if (_requestedMuteStatus != VideoMuteHandler.VideoMuteStatusEnum.Unmuted)
                throw new InvalidOperationException("Mute off isn't requested");

            if (_requestedMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.None ||
                _currentVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Unmuted)
            {
                _requestedMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = Prefix + Commands.Mute + " " + Commands.Off,
            });
        }

        private void ProcessRequestedFreezeStatus()
        {
            if (!PowerIsOnFeedback.BoolValue)
                return;

            switch (_requestedFreezeStatus)
            {
                case VideoFreezeHandler.VideoFreezeStatusEnum.Frozen:
                    ProcessRequestedFreezeOnStatus();
                    break;
                case VideoFreezeHandler.VideoFreezeStatusEnum.Unfrozen:
                    ProcessRequestedFreezeOffStatus();
                    break;
                case VideoFreezeHandler.VideoFreezeStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessRequestedFreezeOnStatus()
        {
            if (_requestedFreezeStatus != VideoFreezeHandler.VideoFreezeStatusEnum.Frozen)
                throw new InvalidOperationException("Freeze on isn't requested");

            if (_requestedFreezeStatus == VideoFreezeHandler.VideoFreezeStatusEnum.None ||
                _currentVideoFreezeStatus == VideoFreezeHandler.VideoFreezeStatusEnum.Frozen)
            {
                _requestedFreezeStatus = VideoFreezeHandler.VideoFreezeStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = Prefix + Commands.Freeze + " " + Commands.On,
            });
        }

        private void ProcessRequestedFreezeOffStatus()
        {
            if (_requestedFreezeStatus != VideoFreezeHandler.VideoFreezeStatusEnum.Unfrozen)
                throw new InvalidOperationException("Freeze off isn't requested");

            if (_requestedFreezeStatus == VideoFreezeHandler.VideoFreezeStatusEnum.None ||
                _currentVideoFreezeStatus == VideoFreezeHandler.VideoFreezeStatusEnum.Unfrozen)
            {
                _requestedFreezeStatus = VideoFreezeHandler.VideoFreezeStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = Prefix + Commands.Freeze + " " + Commands.Off,
            });
        }

        private void ProcessRequestedVideoInput()
        {
            if (!PowerIsOnFeedback.BoolValue)
                return;

            if (_requestedVideoInput == _currentVideoInput)
                _requestedVideoInput = 0;

            if (_requestedVideoInput > 0)
            {
                _commandQueue.Enqueue(new Commands.PJLinkCommand
                {
                    Coms = _coms,
                    Message = Prefix + Commands.Source + " " + _requestedVideoInput.ToString()
                });
            }
        }

        private void HandleVideoInputUpdated(object sender, Events.VideoInputEventArgs videoInputEventArgs)
        {
            _currentVideoInput = videoInputEventArgs.Input;
            ProcessRequestedVideoInput();
            Feedbacks.FireAllFeedbacks();
        }

        public void VideoMuteToggle()
        {
            switch (_currentVideoMuteStatus)
            {
                case VideoMuteHandler.VideoMuteStatusEnum.Muted:
                    VideoMuteOff();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.Unmuted:
                    VideoMuteOn();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.None:
                    VideoMuteOn();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void VideoMuteOn()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.Muted;
            ProcessRequestedMuteStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void VideoMuteOff()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.Unmuted;
            ProcessRequestedMuteStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public BoolFeedback VideoMuteIsOn { get; private set; }

        public void VideoFreezeToggle()
        {
            switch (_currentVideoFreezeStatus)
            {
                case VideoFreezeHandler.VideoFreezeStatusEnum.Frozen:
                    VideoFreezeOff();
                    break;
                case VideoFreezeHandler.VideoFreezeStatusEnum.Unfrozen:
                    VideoFreezeOn();
                    break;
                case VideoFreezeHandler.VideoFreezeStatusEnum.None:
                    VideoFreezeOn();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void VideoFreezeOn()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedFreezeStatus = VideoFreezeHandler.VideoFreezeStatusEnum.Frozen;
            ProcessRequestedFreezeStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void VideoFreezeOff()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedFreezeStatus = VideoFreezeHandler.VideoFreezeStatusEnum.Unfrozen;
            ProcessRequestedFreezeStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public BoolFeedback VideoFreezeIsOn { get; private set; }

        public void PowerOn()
        {
            _requestedPowerStatus = PowerHandler.PowerStatusEnum.PowerOn;
            ProcessRequestedPowerStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void PowerOff()
        {
            _requestedPowerStatus = PowerHandler.PowerStatusEnum.PowerStandby;
            _requestedMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            _requestedFreezeStatus = VideoFreezeHandler.VideoFreezeStatusEnum.None;
            _requestedVideoInput = 0;

            ProcessRequestedPowerStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void PowerToggle()
        {
            switch (_requestedPowerStatus)
            {
                case PowerHandler.PowerStatusEnum.PowerOn:
                    PowerOff();
                    break;

                case PowerHandler.PowerStatusEnum.PowerWarming:
                    break;

                case PowerHandler.PowerStatusEnum.PowerCooling:
                    break;

                case PowerHandler.PowerStatusEnum.PowerStandby:
                    PowerOn();
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            Feedbacks.FireAllFeedbacks();
        }

        public void ExecuteSwitch(object inputSelector)
        {
            try
            {
                var input = Convert.ToUInt32(inputSelector);

                var inputToSwitch = input;
                if (inputToSwitch == 0)
                    return;

                _requestedVideoInput = inputToSwitch;

                PowerOn();
                VideoMuteOff();
                VideoFreezeOff();
                ProcessRequestedVideoInput();
                _pollTimer.Reset(438, _pollTime);
            }
            catch (Exception ex)
            {
                Debug.Console(1, this, "Error executing switch : {0}{1}", ex.Message, ex.StackTrace);
            }
            finally
            {
                Feedbacks.FireAllFeedbacks();
            }
        }

		/// <summary>
		/// Does what it says
		/// </summary>
		public void StartLensMoveRepeat(eLensFunction func)
		{
			if (_LensTimer == null)
			{
				_LensTimer = new CTimer(o => LensFunction(func), null, 0, 250);
			}
		}

		/// <summary>
		/// Does what it says
		/// </summary>
		public void StopLensMoveRepeat()
		{
			if (_LensTimer != null)
			{
				_LensTimer.Stop();
				_LensTimer = null;
			}
		}

		public void LensFunction(eLensFunction function)
		{
			string message; 
			switch (function)
			{
                //case eLensFunction.ZoomPlus:  message = Commands.ZoomInc; break;
                //case eLensFunction.ZoomMinus: message = Commands.ZoomDec; break;
                //case eLensFunction.FocusPlus: message = Commands.FocusInc; break;
                //case eLensFunction.FocusMinus: message = Commands.FocusDec; break;
                //case eLensFunction.HShiftPlus: message = Commands.HLensInc; break;
                //case eLensFunction.HShiftMinus: message = Commands.HLensDec; break;
                //case eLensFunction.VShiftPlus: message = Commands.VLensInc; break;
                //case eLensFunction.VShiftMinus: message = Commands.VLensDec; break;
				default: message = null; break;
				
			}
			if (!string.IsNullOrEmpty(message))
			{
				_commandQueue.Enqueue(new Commands.PJLinkCommand
				{
					Coms = _coms,
                    Message = Prefix + message
				});
			}
		}
		public void LensPositionRecall(ushort memory)
		{
            //if (memory > 0 && memory <= 10)
            //{
            //    _commandQueue.Enqueue(new Commands.PJLinkCommand
            //    {
            //        Coms = _coms,
            //        Message = string.Format("{0}POPLP {1}", _authPrefix, Convert.ToByte(memory))
            //    });
            //}
		}
        public BoolFeedback PowerIsOnFeedback { get; private set; }
        public BoolFeedback PowerIsOffFeedback { get; private set; }
        public BoolFeedback VideoMuteIsOff { get; private set; }
        public BoolFeedback VideoFreezeIsOff { get; private set; }
        public StatusMonitorBase CommunicationMonitor { get; private set; }
        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }
        public BoolFeedback IsWarmingUpFeedback { get; private set; }
        public BoolFeedback IsCoolingDownFeedback { get; private set; }
        public FeedbackCollection<Feedback> Feedbacks { get; private set; }
        public IntFeedback LampHoursFeedback { get; private set; }
        public StringFeedback SerialNumberFeedback { get; private set; }
        public IntFeedback CurrentInputValueFeedback { get; private set; }

        public string CurrentSourceInfoKey { get; set; }
        public SourceListItem CurrentSourceInfo { get; set; }
        public event SourceInfoChangeHandler CurrentSourceChange;

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }
    }
	public enum eLensFunction
	{
		ZoomPlus,
		ZoomMinus,
		ZoomStop,
		FocusPlus,
		FocusMinus,
		FocusStop,
		HShiftPlus,
		HShiftMinus,
		HShiftStop,
		VShiftPlus,
		VShiftMinus,
		VShiftStop,
		Home
	}
	public enum eRemoteControls
	{
		Menu,
		Esc,
		CursorUp,
		CursorDown,
		CursorLeft,
		CursorRight,
		CursorEnter
	}
}