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
    public interface IHasCommandAuthString
    {
        string AuthString { get; set; }
        string ClassString { get; set; }
    }

    public class PJLinkProjector : EssentialsBridgeableDevice, IRoutingSinkWithSwitching, IHasPowerControlWithFeedback,
        IWarmingCooling, IOnline, IBasicVideoMuteWithFeedback, ICommunicationMonitor, IHasFeedback, IBasicVolumeControls
    {
        private CTimer _pollTimer;
        private const int _pollTime = 6000;

        private string ClassString = Commands.Protocol1; //"%1"
        private string AuthString = String.Empty;
        private List<IHasCommandAuthString> DriversRequiringAuthString = new List<IHasCommandAuthString>();

        public PJLinkProjector(string key, string name, PropsConfig config, IBasicCommunication coms) : base(key, name)
        {
            Debug.SetDebugLevel(1);
            _coms = coms;
            if (config.Monitor == null)
                config.Monitor = GetDefaultMonitorConfig();


            CommunicationMonitor = new GenericCommunicationMonitor(this, coms, config.Monitor);
            //DeviceManager.AddDevice(CommunicationMonitor);
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

            AudioMuteIsOn =
                new BoolFeedback("AudioMuteIsOn", () => _currentAudioMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Muted && PowerIsOnFeedback.BoolValue);

            AudioMuteIsOff =
                new BoolFeedback(() => !AudioMuteIsOn.BoolValue && PowerIsOnFeedback.BoolValue);

            VideoFreezeIsOn =
                new BoolFeedback("VideoFreezeIsOn", () => _currentVideoFreezeStatus == VideoFreezeHandler.VideoFreezeStatusEnum.Frozen && PowerIsOnFeedback.BoolValue);

            VideoFreezeIsOff =
                new BoolFeedback(() => !VideoFreezeIsOn.BoolValue && PowerIsOnFeedback.BoolValue);

            var powerHandler = new PowerHandler(key);
            powerHandler.PowerStatusUpdated += HandlePowerStatusUpdated;

            var muteHandler = new VideoMuteHandler(key);
            muteHandler.VideoMuteStatusUpdated += HandleVideoMuteStatusUpdated;
            muteHandler.AudioMuteStatusUpdated += HandleAudioMuteStatusUpdated;
            
            var freezeHandler = new VideoFreezeHandler(key);
            freezeHandler.VideoFreezeStatusUpdated += HandleFreezeStatusUpdated;

            var inputHandler = new VideoInputHandler(key);
            inputHandler.VideoInputUpdated += HandleVideoInputUpdated;

            var authHandler = new AuthHandler(key);
            authHandler.AuthUpdated += HandleAuthUpdated;

            var classHandler = new ClassHandler(key);
            classHandler.ClassUpdated += HandleClassUpdated;

            new StringResponseProcessor(gather,
                s =>
                {
                        classHandler.ProcessResponse(s);
                        authHandler.ProcessResponse(s);
                        powerHandler.ProcessResponse(s);
                        muteHandler.ProcessResponse(s);
                        freezeHandler.ProcessResponse(s);
                        inputHandler.ProcessResponse(s);
                    });

            LampHoursFeedback =
                new LampHoursHandler(key, _commandQueue, gather, PowerIsOnFeedback).LampHoursFeedback;
            DriversRequiringAuthString.Add(LampHoursFeedback as IHasCommandAuthString);

            SerialNumberFeedback =
                new SerialNumberHandler(key, _commandQueue, gather, PowerIsOnFeedback).SerialNumberFeedback;
            DriversRequiringAuthString.Add(SerialNumberFeedback as IHasCommandAuthString);

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
                AudioMuteIsOn,
                AudioMuteIsOff,
                VideoFreezeIsOn,
                VideoFreezeIsOff,
                CurrentInputValueFeedback,
                new StringFeedback("RequestedPower", () => _requestedPowerStatus.ToString()),
                new StringFeedback("RequestedMute", () => _requestedVideoMuteStatus.ToString()),
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

        public override bool CustomActivate()
        {
            Feedbacks.RegisterForConsoleUpdates(this);
            Feedbacks.FireAllFeedbacks();

            _pollTimer = new CTimer(o =>
                {
                    Debug.Console(2, this, "Polling, IsOnline: {0}, Status: {1}, IsConnected: {2}, ", CommunicationMonitor.IsOnlineFeedback.BoolValue, CommunicationMonitor.Status, _coms.IsConnected);
                    if (!CommunicationMonitor.IsOnlineFeedback.BoolValue)
                    {
                        CommunicationMonitor.Stop();
                        CommunicationMonitor.Start();
                    }
                    if(!_coms.IsConnected)
                        _coms.Connect();
                    
                    GetPower();

                    if (!PowerIsOnFeedback.BoolValue)
                        return;

                    _commandQueue.Enqueue(new Commands.PJLinkCommand
                    {
                        Coms = _coms,
                        Message = MakeQueryCommand(Commands.Source)
                    });

                    _commandQueue.Enqueue(new Commands.PJLinkCommand
                    {
                        Coms = _coms,
                        Message = MakeQueryCommand(Commands.Mute)
                    });

                    if (ClassString.Equals(Commands.Protocol2))
                    {
                        _commandQueue.Enqueue(new Commands.PJLinkCommand
                        {
                            Coms = _coms,
                            Message = MakeQueryCommand(Commands.Freeze)
                        });
                    }
                },
                null,
                5189,
                _pollTime);

            PowerIsOnFeedback.OutputChange += (sender, args) =>
                {
                    if (!args.BoolValue)
                        return;

                    ProcessRequestedVideoInput();
                    ProcessRequestedVideoMuteStatus();
                    ProcessRequestedFreezeStatus();
                };

            CommunicationMonitor.StatusChange += new EventHandler<MonitorStatusChangeEventArgs>(CommunicationMonitor_StatusChange);
            CommunicationMonitor.Start();
            if (!_coms.IsConnected)
                _coms.Connect();
            Debug.Console(1, this, "CommunicationMonitor {0} Start, IsOnline: {1}", CommunicationMonitor.Key, CommunicationMonitor.IsOnlineFeedback.BoolValue);
            var device_ = DeviceManager.GetDeviceForKey(CommunicationMonitor.Key);
            if(device_ != null)
                Debug.Console(2, this, "CommunicationMonitor key: {0}", device_.Key);
            return base.CustomActivate();
        }

        public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
        {
            var joinMap = new JoinMap(joinStart);
            if (bridge != null)
                bridge.AddJoinMap(Key, joinMap);

            Bridge.LinkToApi(this, trilist, joinMap);
        }

        public string MakeCommand(string command)
        {
            //Debug.Console(0, this, "MakeCommand: {0}:{1}:{2}", AuthString, ClassString, command);
            var msg_ = String.Format("{0}{1}{2}", AuthString, ClassString, command);
            //Debug.Console(0, this, "MakeCommand msg_: {0}", msg_);
            return msg_;
        }
        public string MakeQueryCommand(string command)
        {
            //Debug.Console(0, this, "MakeQueryCommand: {0}:{1}:{2}:{3}", AuthString, ClassString, command, Commands.Query);
            var msg_ = String.Format("{0}{1}{2}{3}", AuthString, ClassString, command, Commands.Query);
            //Debug.Console(0, this, "MakeQueryCommand msg_: '{0}'", msg_);
            return msg_;
        }

        #region power

        private PowerHandler.PowerStatusEnum _currentPowerStatus;
        private PowerHandler.PowerStatusEnum _requestedPowerStatus;
 
        private void HandlePowerStatusUpdated(object sender, Events.PowerEventArgs eventArgs)
        {
            Debug.Console(2, this, "HandlePowerStatusUpdated Status: '{0}'", eventArgs.Status);
            _currentPowerStatus = eventArgs.Status;
            ProcessRequestedPowerStatus();
            Feedbacks.FireAllFeedbacks();
        }

        private void ProcessRequestedPowerStatus()
        {
            Debug.Console(2, this, "ProcessRequestedPowerStatus() _requestedPowerStatus: {0}", _requestedPowerStatus.ToString());
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
                case PowerHandler.PowerStatusEnum.Error:
                    break;
                default:
                    Debug.Console(1, this, "ProcessRequestedPowerStatus() ERROR, _requestedPowerStatus: {0}, not handled", _requestedPowerStatus.ToString());
                    //throw new ArgumentOutOfRangeException();
                    break;
            }
        }

        private void ProcessRequestedPowerOn()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn)
                Debug.Console(1, this, "ProcessRequestedPowerOn() Power on isn't requested. Current request: {0}", _requestedPowerStatus.ToString());
                //throw new InvalidOperationException("Power on isn't requested");

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

                case PowerHandler.PowerStatusEnum.Error:
                    break;
                case PowerHandler.PowerStatusEnum.None:
                    break;
                default:
                    Debug.Console(1, this, "ProcessRequestedPowerOn() ERROR, _requestedPowerStatus: {0}, not handled", _requestedPowerStatus.ToString());
                    //throw new ArgumentOutOfRangeException();
                    break;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeCommand(Commands.Power + " " + Commands.On),
            });
        }

        private void ProcessRequestedPowerOff()
        {
            //Debug.Console(0, this, "ProcessRequestedPowerOff()");
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerStandby)
                Debug.Console(1, this, "ProcessRequestedPowerOff() Power off isn't requested. Current request: {0}", _requestedPowerStatus.ToString());
                //throw new InvalidOperationException("Power off isn't requested");

            //Debug.Console(1, this, "ProcessRequestedPowerOff() _currentPowerStatus: {0}", _currentPowerStatus.ToString());
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
                case PowerHandler.PowerStatusEnum.Error:
                    _requestedPowerStatus = PowerHandler.PowerStatusEnum.None;
                    break;
                case PowerHandler.PowerStatusEnum.None:
                    break;
                default:
                    Debug.Console(1, this, "ProcessRequestedPowerOff() ERROR _currentPowerStatus: {0}, not handled", _currentPowerStatus.ToString());
                    //throw new ArgumentOutOfRangeException();
                    break;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeCommand(Commands.Power + " " + Commands.Off),
            });
        }

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
            _requestedVideoMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            _requestedAudioMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            _requestedFreezeStatus = VideoFreezeHandler.VideoFreezeStatusEnum.None;
            _requestedVideoInput = 0;

            ProcessRequestedPowerStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void PowerToggle()
        {
            //Debug.Console(0, this, "PowerToggle pressed, _requestedPowerStatus: {0}", _requestedPowerStatus);
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
                    switch (_currentPowerStatus)
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

                        case PowerHandler.PowerStatusEnum.Error:
                            PowerOn();
                            break;

                        case PowerHandler.PowerStatusEnum.None:
                            PowerOn();
                            break;
                        
                        default:
                            Debug.Console(1, this, "PowerToggle() ERROR _currentPowerStatus: {0}, not handled", _currentPowerStatus.ToString());
                            //throw new ArgumentOutOfRangeException();
                            break;
                    }
                    break;
            }

            Feedbacks.FireAllFeedbacks();
        }

        private void GetPower()
        {
            try
            {
                var msg_ = MakeQueryCommand(Commands.Power);
                Debug.Console(2, this, "GetPower('{0}')", msg_);
                _commandQueue.Enqueue(new Commands.PJLinkCommand
                {
                    Coms = _coms,
                    Message = msg_
                });
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "GetPower ERROR: {0}", e.Message);
            }
        }

        public BoolFeedback PowerIsOnFeedback { get; private set; }
        public BoolFeedback PowerIsOffFeedback { get; private set; }
        public BoolFeedback IsWarmingUpFeedback { get; private set; }
        public BoolFeedback IsCoolingDownFeedback { get; private set; }

        #endregion power
        #region video input

        private uint _currentVideoInput;
        private uint _requestedVideoInput;

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
                AudioMuteOff();
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
                    Message = MakeCommand(Commands.Source + " " + _requestedVideoInput.ToString())
                });
            }
        }

        private void HandleVideoInputUpdated(object sender, Events.VideoInputEventArgs videoInputEventArgs)
        {
            _currentVideoInput = videoInputEventArgs.Input;
            //ProcessRequestedVideoInput();
            //if (CurrentSourceChange != null)
            //    CurrentSourceChange(CurrentSourceInfo, ChangeType.DidChange);
            Feedbacks.FireAllFeedbacks();
        }

        public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }
        public IntFeedback CurrentInputValueFeedback { get; private set; }

        public string CurrentSourceInfoKey { get; set; }
        public SourceListItem CurrentSourceInfo { get; set; }
        public event SourceInfoChangeHandler CurrentSourceChange; // this needs to be implemented by the parent program because it contains info on external devices unknown to the display.

        #endregion video input
        #region video mute

        private VideoMuteHandler.VideoMuteStatusEnum _currentVideoMuteStatus;
        private VideoMuteHandler.VideoMuteStatusEnum _requestedVideoMuteStatus;

        private void HandleVideoMuteStatusUpdated(object sender, Events.VideoMuteEventArgs videoMuteEventArgs)
        {
            _currentVideoMuteStatus = videoMuteEventArgs.Status;

            ProcessRequestedVideoMuteStatus();
            Feedbacks.FireAllFeedbacks();
        }

        private void ProcessRequestedVideoMuteStatus()
        {
            if (!PowerIsOnFeedback.BoolValue)
                return;

            switch (_requestedVideoMuteStatus)
            {
                case VideoMuteHandler.VideoMuteStatusEnum.Muted:
                    ProcessRequestedVideoMuteOnStatus();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.Unmuted:
                    ProcessRequestedVideoMuteOffStatus();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessRequestedVideoMuteOnStatus()
        {
            if (_requestedVideoMuteStatus != VideoMuteHandler.VideoMuteStatusEnum.Muted)
                throw new InvalidOperationException("Video Mute on isn't requested");

            if (_requestedVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.None ||
                _currentVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Muted)
            {
                _requestedVideoMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeCommand(Commands.Mute + " " + Commands.Video + Commands.On),
            });
        }

        private void ProcessRequestedVideoMuteOffStatus()
        {
            if (_requestedVideoMuteStatus != VideoMuteHandler.VideoMuteStatusEnum.Unmuted)
                throw new InvalidOperationException("Video Mute off isn't requested");

            if (_requestedVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.None ||
                _currentVideoMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Unmuted)
            {
                _requestedVideoMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeCommand(Commands.Mute + " " + Commands.Video + Commands.Off),
            });
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

            _requestedVideoMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.Muted;
            ProcessRequestedVideoMuteStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void VideoMuteOff()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedVideoMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.Unmuted;
            ProcessRequestedVideoMuteStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public BoolFeedback VideoMuteIsOn { get; private set; }
        public BoolFeedback VideoMuteIsOff { get; private set; }
        
        #endregion video mute
        #region audio mute

        private VideoMuteHandler.VideoMuteStatusEnum _currentAudioMuteStatus;
        private VideoMuteHandler.VideoMuteStatusEnum _requestedAudioMuteStatus;
 
        private void HandleAudioMuteStatusUpdated(object sender, Events.VideoMuteEventArgs audioMuteEventArgs)
        {
            _currentAudioMuteStatus = audioMuteEventArgs.Status;

            ProcessRequestedAudioMuteStatus();
            Feedbacks.FireAllFeedbacks();
        }

        private void ProcessRequestedAudioMuteStatus()
        {
            if (!PowerIsOnFeedback.BoolValue)
                return;

            switch (_requestedAudioMuteStatus)
            {
                case VideoMuteHandler.VideoMuteStatusEnum.Muted:
                    ProcessRequestedAudioMuteOnStatus();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.Unmuted:
                    ProcessRequestedAudioMuteOffStatus();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.None:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void ProcessRequestedAudioMuteOnStatus()
        {
            if (_requestedAudioMuteStatus != VideoMuteHandler.VideoMuteStatusEnum.Muted)
                throw new InvalidOperationException("Audio Mute on isn't requested");

            if (_requestedAudioMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.None ||
                _currentAudioMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Muted)
            {
                _requestedAudioMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeCommand(Commands.Mute + " " + Commands.Audio + Commands.On)
            });
        }

        private void ProcessRequestedAudioMuteOffStatus()
        {
            if (_requestedAudioMuteStatus != VideoMuteHandler.VideoMuteStatusEnum.Unmuted)
                throw new InvalidOperationException("Audio Mute off isn't requested");

            if (_requestedAudioMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.None ||
                _currentAudioMuteStatus == VideoMuteHandler.VideoMuteStatusEnum.Unmuted)
            {
                _requestedAudioMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.None;
            }

            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeCommand(Commands.Mute + " " + Commands.Audio + Commands.Off)
            });
        }

        public void AudioMuteToggle()
        {
            switch (_currentAudioMuteStatus)
            {
                case VideoMuteHandler.VideoMuteStatusEnum.Muted:
                    AudioMuteOff();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.Unmuted:
                    AudioMuteOn();
                    break;
                case VideoMuteHandler.VideoMuteStatusEnum.None:
                    AudioMuteOn();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void AudioMuteOn()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedAudioMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.Muted;
            ProcessRequestedAudioMuteStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public void AudioMuteOff()
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;

            _requestedAudioMuteStatus = VideoMuteHandler.VideoMuteStatusEnum.Unmuted;
            ProcessRequestedAudioMuteStatus();
            Feedbacks.FireAllFeedbacks();
            _pollTimer.Reset(329, _pollTime);
        }

        public BoolFeedback AudioMuteIsOn { get; private set; }
        public BoolFeedback AudioMuteIsOff { get; private set; }

        #endregion audio 
        #region volume
        // there is no level feedback in the pjlink specifiaction
        public void MuteToggle()
        {
            AudioMuteToggle();
        }

        public void VolumeDown(bool state)
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;
            if (state)
            {
                _commandQueue.Enqueue(new Commands.PJLinkCommand
                {
                    Coms = _coms,
                    Message = MakeCommand(Commands.Volume + " " + Commands.Down),
                });
            }
        }

        public void VolumeUp(bool state)
        {
            if (_requestedPowerStatus != PowerHandler.PowerStatusEnum.PowerOn && !PowerIsOnFeedback.BoolValue)
                return;
            if (state)
            {
                _commandQueue.Enqueue(new Commands.PJLinkCommand
                {
                    Coms = _coms,
                });
            }
        }

        #endregion volume
        #region video freeze

        private VideoFreezeHandler.VideoFreezeStatusEnum _currentVideoFreezeStatus;
        private VideoFreezeHandler.VideoFreezeStatusEnum _requestedFreezeStatus;

        private void HandleFreezeStatusUpdated(object sender, Events.VideoFreezeEventArgs videoFreezeEventArgs)
        {
            _currentVideoFreezeStatus = videoFreezeEventArgs.Status;

            ProcessRequestedFreezeStatus();
            Feedbacks.FireAllFeedbacks();
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

            if (ClassString.Equals(Commands.Protocol2))
            {
                _commandQueue.Enqueue(new Commands.PJLinkCommand
                {
                    Coms = _coms,
                    Message = MakeCommand(Commands.Freeze + " " + Commands.On),
                }); 
            }

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

            if (ClassString.Equals(Commands.Protocol2))
            {
                _commandQueue.Enqueue(new Commands.PJLinkCommand
                {
                    Coms = _coms,
                    Message = MakeCommand(Commands.Freeze + " " + Commands.Off),
                });
            }

        }

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
        public BoolFeedback VideoFreezeIsOff { get; private set; }

        #endregion video freeze
        #region class
        private void UpdateDriversRequiringAuthString(string authString, string classString)
        {
            try
            {
               if (DriversRequiringAuthString != null)
                {
                    foreach (var driver in DriversRequiringAuthString)
                    {
                        //Debug.Console(0, this, "HandleAuthStatusUpdated driver {0}", driver == null ? "== null" : driver.GetType().ToString());
                        if (authString != null)
                            driver.AuthString = authString;
                        if (classString != null) 
                            driver.ClassString = classString;
                    }
                } 
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "UpdateDriversRequiringAuthString ERROR: {0}", e.Message);
            }
        }

        private void HandleClassUpdated(object sender, Events.StringEventArgs eventArgs)
        {
            try
            {
                //Debug.Console(1, this, "HandleClassUpdated()");
                if (eventArgs == null)
                    Debug.Console(1, this, "HandleClassUpdated eventArgs == null");
                else if (eventArgs.Val == null)
                    Debug.Console(1, this, "HandleClassUpdated eventArgs.Val == null");
                else
                    Debug.Console(1, this, "HandleClassUpdated({0})", eventArgs.Val);

                ClassString = "%" + eventArgs.Val;
                UpdateDriversRequiringAuthString(AuthString, ClassString);
                GetPower();
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "HandleClassUpdated ERROR: {0}", e.Message);
            }
        }

        private void GetClass()
        {
            Debug.Console(0, this, "GetClass()");
            _commandQueue.Enqueue(new Commands.PJLinkCommand
            {
                Coms = _coms,
                Message = MakeQueryCommand(Commands.Class)
            });
        }

        #endregion class
        #region authentication

        private void HandleAuthUpdated(object sender, Events.StringEventArgs eventArgs)
        {
            try
            {
                if (eventArgs.Val == null)
                {
                    AuthString = String.Empty;
                    Debug.Console(0, this, "HandleAuthUpdated, eventArgs.Val == null", eventArgs.Val);
                }
                else
                    AuthString = eventArgs.Val;
                GetClass();
            }
            catch (Exception e)
            {
                Debug.Console(0, this, "HandleAuthUpdated ERROR: {0}", e.Message);
            }
        }


        #endregion authentication
        #region serial number

        public StringFeedback SerialNumberFeedback { get; private set; }

        #endregion serial number
        #region lamp hours

        public IntFeedback LampHoursFeedback { get; private set; }

        #endregion lamp hours
        #region comms

        private readonly IBasicCommunication _coms;
        private readonly GenericQueue _commandQueue;

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

        void CommunicationMonitor_StatusChange(object sender, MonitorStatusChangeEventArgs e)
        {
            Debug.Console(0, this, "CommunicationMonitor_StatusChange: {0} - {1}", e.Status, e.Message);
        }

        public StatusMonitorBase CommunicationMonitor { get; private set; }

        public BoolFeedback IsOnline
        {
            get { return CommunicationMonitor.IsOnlineFeedback; }
        }

        #endregion comms

        public FeedbackCollection<Feedback> Feedbacks { get; private set; }

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