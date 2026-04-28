using System;

namespace LaunchPlugin
{
    internal enum PitTyreControlMode
    {
        Off = 0,
        Dry = 1,
        Wet = 2,
        Auto = 3
    }

    internal sealed class PitTyreControlSnapshot
    {
        public bool HasTireServiceSelection;
        public bool IsTireServiceSelected;
        public bool HasRequestedCompound;
        public int RequestedCompound;
        public bool HasPlayerCompound;
        public int PlayerCompound;
        public bool WeatherDeclaredWet;
        public string AvailableCompound01 = string.Empty;
        public string AvailableCompound02 = string.Empty;
    }

    internal sealed class PitTyreControlEngine
    {
        private const int SettleWindowMs = 1000;
        private const int SendFailureHoldMs = 1000;

        private readonly Func<PitTyreControlSnapshot> _snapshotProvider;
        private readonly Func<string, string, string, bool> _rawCommandSender;
        private readonly Action<string, string, string> _feedbackPublisher;
        private readonly Action<string> _logger;

        private DateTime _settleUntilUtc = DateTime.MinValue;
        private DateTime _sendFailureHoldUntilUtc = DateTime.MinValue;
        private bool _autoPendingInitialEvaluation;
        private bool? _autoLastDesiredWet;
        private bool _hasLastTruthMode;
        private PitTyreControlMode _lastTruthMode = PitTyreControlMode.Off;

        public PitTyreControlMode Mode { get; private set; } = PitTyreControlMode.Off;
        public int Fault { get; private set; }

        public PitTyreControlEngine(
            Func<PitTyreControlSnapshot> snapshotProvider,
            Func<string, string, string, bool> rawCommandSender,
            Action<string, string, string> feedbackPublisher,
            Action<string> logger)
        {
            _snapshotProvider = snapshotProvider;
            _rawCommandSender = rawCommandSender;
            _feedbackPublisher = feedbackPublisher;
            _logger = logger;
        }

        public string ModeText => ModeToText(Mode);

        public void ResetToOff()
        {
            Mode = PitTyreControlMode.Off;
            _settleUntilUtc = DateTime.MinValue;
            _sendFailureHoldUntilUtc = DateTime.MinValue;
            _autoPendingInitialEvaluation = false;
            _autoLastDesiredWet = null;
            _hasLastTruthMode = false;
            _lastTruthMode = PitTyreControlMode.Off;
            Fault = 0;
        }

        public void ModeCycle()
        {
            switch (Mode)
            {
                case PitTyreControlMode.Off:
                    SetDry();
                    break;
                case PitTyreControlMode.Dry:
                    SetWet();
                    break;
                case PitTyreControlMode.Wet:
                    SetAuto();
                    break;
                default:
                    SetOff();
                    break;
            }
        }

        public void SetOff()
        {
            Mode = PitTyreControlMode.Off;
            _autoPendingInitialEvaluation = false;
            _autoLastDesiredWet = null;
            SendModeCommand("Pit.TyreControl.SetOff", "#cleartires", "TYRE CHANGE OFF", "driver-action", PitTyreControlMode.Off);
        }

        public void SetDry()
        {
            Mode = PitTyreControlMode.Dry;
            _autoPendingInitialEvaluation = false;
            _autoLastDesiredWet = null;
            SendModeCommand("Pit.TyreControl.SetDry", "#t tc 0", "TYRE CHANGE DRY", "driver-action", PitTyreControlMode.Dry);
        }

        public void SetWet()
        {
            Mode = PitTyreControlMode.Wet;
            _autoPendingInitialEvaluation = false;
            _autoLastDesiredWet = null;
            SendModeCommand("Pit.TyreControl.SetWet", "#t tc 2", "TYRE CHANGE WET", "driver-action", PitTyreControlMode.Wet);
        }

        public void SetAuto()
        {
            Mode = PitTyreControlMode.Auto;
            _autoPendingInitialEvaluation = true;
            _autoLastDesiredWet = null;
            _feedbackPublisher?.Invoke("Pit.TyreControl.SetAuto", "TYRE CHANGE AUTO", string.Empty);
            _logger?.Invoke("[LalaPlugin:PitTyreControl] mode=AUTO reason=driver-action command=none");
        }

        public void OnTelemetryTick()
        {
            var snapshot = _snapshotProvider();
            if (snapshot == null)
            {
                Fault = 0;
                return;
            }

            PitTyreControlMode truthMode;
            bool hasTruth = TryMapManualTruthMode(snapshot, out truthMode);
            bool inSettleWindow = DateTime.UtcNow < _settleUntilUtc;
            bool inSendFailureHoldWindow = DateTime.UtcNow < _sendFailureHoldUntilUtc;
            bool remapAppliedThisTick = false;
            bool autoCorrectionSendIssuedThisTick = false;

            if (Mode == PitTyreControlMode.Auto)
            {
                remapAppliedThisTick = HandleAuto(snapshot, hasTruth, truthMode, inSettleWindow, out autoCorrectionSendIssuedThisTick);
            }
            else
            {
                _autoPendingInitialEvaluation = false;
                _autoLastDesiredWet = null;

                if (!inSettleWindow && hasTruth && truthMode != Mode)
                {
                    Mode = truthMode;
                    remapAppliedThisTick = true;
                    if (!inSendFailureHoldWindow)
                    {
                        _feedbackPublisher?.Invoke("Pit.TyreControl.TruthMirror", $"TYRE {ModeToText(Mode)}", string.Empty);
                    }
                    _logger?.Invoke($"[LalaPlugin:PitTyreControl] mode={ModeToText(Mode)} reason=truth-mirror serviceKnown={snapshot.HasTireServiceSelection} serviceOn={snapshot.IsTireServiceSelected} requestedCompound={(snapshot.HasRequestedCompound ? snapshot.RequestedCompound.ToString() : "NA")}");
                }
            }

            UpdateTruthHistory(hasTruth, truthMode);

            if (remapAppliedThisTick)
            {
                Fault = 0;
                return;
            }

            if (autoCorrectionSendIssuedThisTick)
            {
                Fault = 0;
                return;
            }

            bool inSettleWindowAfterHandlers = DateTime.UtcNow < _settleUntilUtc;
            Fault = ComputeFault(snapshot, hasTruth, truthMode, inSettleWindowAfterHandlers);
        }

        private bool HandleAuto(PitTyreControlSnapshot snapshot, bool hasTruth, PitTyreControlMode truthMode, bool inSettleWindow, out bool autoCorrectionSendIssuedThisTick)
        {
            autoCorrectionSendIssuedThisTick = false;
            bool desiredWet = snapshot.WeatherDeclaredWet;
            PitTyreControlMode desiredMode = desiredWet ? PitTyreControlMode.Wet : PitTyreControlMode.Dry;
            bool desiredChanged = !_autoLastDesiredWet.HasValue || _autoLastDesiredWet.Value != desiredWet;

            if (inSettleWindow)
            {
                _autoLastDesiredWet = desiredWet;
                return false;
            }

            if (desiredChanged || _autoPendingInitialEvaluation)
            {
                if (!hasTruth)
                {
                    return false;
                }

                _autoLastDesiredWet = desiredWet;

                if (truthMode != desiredMode)
                {
                    if (desiredMode == PitTyreControlMode.Wet)
                    {
                        SendModeCommand("Pit.TyreControl.Auto.CorrectionWet", "#t tc 2", "TYRE AUTO CHANGE WET", "auto-correction", PitTyreControlMode.Auto);
                    }
                    else
                    {
                        SendModeCommand("Pit.TyreControl.Auto.CorrectionDry", "#t tc 0", "TYRE AUTO CHANGE DRY", "auto-correction", PitTyreControlMode.Auto);
                    }

                    autoCorrectionSendIssuedThisTick = true;
                }

                _autoPendingInitialEvaluation = false;
                return false;
            }

            if (hasTruth && _hasLastTruthMode)
            {
                bool truthChanged = truthMode != _lastTruthMode;
                if (truthChanged && truthMode != desiredMode)
                {
                    Mode = truthMode;
                    _autoPendingInitialEvaluation = false;
                    _autoLastDesiredWet = null;
                    _feedbackPublisher?.Invoke("Pit.TyreControl.Auto.Cancel", "TYRE AUTO CANCELLED", string.Empty);
                    _logger?.Invoke($"[LalaPlugin:PitTyreControl] mode={ModeToText(Mode)} reason=auto-cancel serviceKnown={snapshot.HasTireServiceSelection} serviceOn={snapshot.IsTireServiceSelected} requestedCompound={(snapshot.HasRequestedCompound ? snapshot.RequestedCompound.ToString() : "NA")}");
                    return true;
                }
            }

            return false;
        }

        private void SendModeCommand(string actionName, string command, string feedbackText, string reason, PitTyreControlMode modeForLog)
        {
            bool sent = _rawCommandSender?.Invoke(actionName, command, feedbackText) ?? false;
            _logger?.Invoke($"[LalaPlugin:PitTyreControl] mode={ModeToText(modeForLog)} reason={reason} command='{command}' sent={sent}");
            if (sent)
            {
                _settleUntilUtc = DateTime.UtcNow.AddMilliseconds(SettleWindowMs);
                _sendFailureHoldUntilUtc = DateTime.MinValue;
            }
            else
            {
                _sendFailureHoldUntilUtc = DateTime.UtcNow.AddMilliseconds(SendFailureHoldMs);
            }
        }

        private void UpdateTruthHistory(bool hasTruth, PitTyreControlMode truthMode)
        {
            if (!hasTruth)
            {
                _hasLastTruthMode = false;
                return;
            }

            _hasLastTruthMode = true;
            _lastTruthMode = truthMode;
        }

        private static bool TryMapManualTruthMode(PitTyreControlSnapshot snapshot, out PitTyreControlMode truthMode)
        {
            truthMode = PitTyreControlMode.Off;

            if (!snapshot.HasTireServiceSelection)
            {
                return false;
            }

            if (!snapshot.IsTireServiceSelected)
            {
                truthMode = PitTyreControlMode.Off;
                return true;
            }

            if (!snapshot.HasRequestedCompound)
            {
                return false;
            }

            if (IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, false))
            {
                truthMode = PitTyreControlMode.Dry;
                return true;
            }

            if (IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, true))
            {
                truthMode = PitTyreControlMode.Wet;
                return true;
            }

            return false;
        }

        private static bool IsRequestedCompoundInDesiredFamily(int requestedCompound, bool desiredWet)
        {
            if (desiredWet)
            {
                return requestedCompound == 1;
            }

            return requestedCompound == 0;
        }

        private int ComputeFault(PitTyreControlSnapshot snapshot, bool hasTruth, PitTyreControlMode truthMode, bool inSettleWindow)
        {
            if (snapshot == null || inSettleWindow)
            {
                return 0;
            }

            if (snapshot.HasTireServiceSelection &&
                snapshot.IsTireServiceSelected &&
                snapshot.HasRequestedCompound &&
                !hasTruth)
            {
                return 0;
            }

            if (Mode == PitTyreControlMode.Auto && _autoPendingInitialEvaluation && !hasTruth)
            {
                return 0;
            }

            bool modeFault = false;
            bool requestFault = false;

            if (Mode == PitTyreControlMode.Off)
            {
                if (snapshot.HasTireServiceSelection)
                {
                    modeFault = snapshot.IsTireServiceSelected;
                    requestFault = snapshot.IsTireServiceSelected;
                }
            }
            else if (Mode == PitTyreControlMode.Dry || Mode == PitTyreControlMode.Wet)
            {
                bool desiredWet = Mode == PitTyreControlMode.Wet;
                if (!snapshot.HasTireServiceSelection || !snapshot.HasRequestedCompound)
                {
                    return 0;
                }

                if (!hasTruth)
                {
                    return 0;
                }

                modeFault = truthMode != Mode;
                requestFault = !snapshot.IsTireServiceSelected || !IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, desiredWet);
            }
            else
            {
                bool desiredWet = snapshot.WeatherDeclaredWet;
                PitTyreControlMode desiredMode = desiredWet ? PitTyreControlMode.Wet : PitTyreControlMode.Dry;
                if (!snapshot.HasTireServiceSelection || !snapshot.HasRequestedCompound || !hasTruth)
                {
                    return 0;
                }

                modeFault = truthMode != desiredMode;
                requestFault = !snapshot.IsTireServiceSelected || !IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, desiredWet);
            }

            int value = 0;
            if (modeFault)
            {
                value |= 1;
            }

            if (requestFault)
            {
                value |= 2;
            }

            return value;
        }

        private static string ModeToText(PitTyreControlMode mode)
        {
            switch (mode)
            {
                case PitTyreControlMode.Off:
                    return "OFF";
                case PitTyreControlMode.Dry:
                    return "DRY";
                case PitTyreControlMode.Wet:
                    return "WET";
                case PitTyreControlMode.Auto:
                    return "AUTO";
                default:
                    return "OFF";
            }
        }
    }
}
