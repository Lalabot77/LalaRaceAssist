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
        private const int SendCooldownMs = 900;
        private const int MaxAttemptsPerTarget = 2;
        private const int ManualConfirmationWindowMs = 1400;
        private const int ManualReconcileCooldownMs = 700;

        private readonly Func<PitTyreControlSnapshot> _snapshotProvider;
        private readonly Func<string, string, string, bool> _rawCommandSender;
        private readonly Action<PitCommandAction> _builtInActionSender;
        private readonly Action<string, string, string> _feedbackPublisher;
        private readonly Action<string> _logger;

        private string _lastServiceTargetKey = string.Empty;
        private int _serviceTargetAttempts;
        private DateTime _serviceNextAttemptUtc = DateTime.MinValue;

        private string _lastCompoundTargetKey = string.Empty;
        private int _compoundTargetAttempts;
        private DateTime _compoundNextAttemptUtc = DateTime.MinValue;

        private bool _manualConfirmationPending;
        private DateTime _manualConfirmationDeadlineUtc = DateTime.MinValue;
        private DateTime _manualNextReconcileUtc = DateTime.MinValue;

        private string _autoServiceFailureNoticeKey = string.Empty;
        private string _autoCompoundFailureNoticeKey = string.Empty;

        public PitTyreControlMode Mode { get; private set; } = PitTyreControlMode.Off;

        public PitTyreControlEngine(
            Func<PitTyreControlSnapshot> snapshotProvider,
            Func<string, string, string, bool> rawCommandSender,
            Action<PitCommandAction> builtInActionSender,
            Action<string, string, string> feedbackPublisher,
            Action<string> logger)
        {
            _snapshotProvider = snapshotProvider;
            _rawCommandSender = rawCommandSender;
            _builtInActionSender = builtInActionSender;
            _feedbackPublisher = feedbackPublisher;
            _logger = logger;
        }

        public string ModeText => ModeToText(Mode);

        public void ResetToOff()
        {
            Mode = PitTyreControlMode.Off;
            ResetAttemptState();
            ResetManualSyncState();
            ResetAutoFailureNoticeState();
        }

        public void ModeCycle()
        {
            switch (Mode)
            {
                case PitTyreControlMode.Off:
                    SetMode(PitTyreControlMode.Dry, "Pit.TyreControl.ModeCycle");
                    break;
                case PitTyreControlMode.Dry:
                    SetMode(PitTyreControlMode.Wet, "Pit.TyreControl.ModeCycle");
                    break;
                case PitTyreControlMode.Wet:
                    SetMode(PitTyreControlMode.Auto, "Pit.TyreControl.ModeCycle");
                    break;
                default:
                    SetMode(PitTyreControlMode.Off, "Pit.TyreControl.ModeCycle");
                    break;
            }
        }

        public void SetOff() => SetMode(PitTyreControlMode.Off, "Pit.TyreControl.SetOff");
        public void SetDry() => SetMode(PitTyreControlMode.Dry, "Pit.TyreControl.SetDry");
        public void SetWet() => SetMode(PitTyreControlMode.Wet, "Pit.TyreControl.SetWet");
        public void SetAuto() => SetMode(PitTyreControlMode.Auto, "Pit.TyreControl.SetAuto");

        public void OnTelemetryTick()
        {
            var snapshot = _snapshotProvider();
            if (snapshot == null)
            {
                return;
            }

            if (Mode == PitTyreControlMode.Auto)
            {
                bool desiredService = true;
                bool desiredWet = snapshot.WeatherDeclaredWet;
                EnsureTyreService(snapshot, desiredService);
                EnsureCompound(snapshot, desiredWet);
                HandleAutoFailureFeedback(snapshot, desiredService, desiredWet);
                return;
            }

            TryReconcileManualModeToTruth(snapshot);

            if (Mode == PitTyreControlMode.Off)
            {
                EnsureTyreService(snapshot, false);
                ResetCompoundAttempts();
                return;
            }

            EnsureTyreService(snapshot, true);
            bool desiredWet = Mode == PitTyreControlMode.Wet;
            EnsureCompound(snapshot, desiredWet);
        }

        private void SetMode(PitTyreControlMode mode, string actionName)
        {
            Mode = mode;
            ResetAttemptState();
            ResetAutoFailureNoticeState();
            BeginOrClearManualConfirmation(mode);
            PublishSelectionFeedback(actionName, string.Format("TYRE MODE {0}", ModeToText(mode)));
        }

        private void BeginOrClearManualConfirmation(PitTyreControlMode mode)
        {
            if (mode == PitTyreControlMode.Auto)
            {
                ResetManualSyncState();
                return;
            }

            _manualConfirmationPending = true;
            _manualConfirmationDeadlineUtc = DateTime.UtcNow.AddMilliseconds(ManualConfirmationWindowMs);
            _manualNextReconcileUtc = _manualConfirmationDeadlineUtc;
        }

        private void TryReconcileManualModeToTruth(PitTyreControlSnapshot snapshot)
        {
            PitTyreControlMode truthMode;
            bool hasTruth = TryMapManualTruthMode(snapshot, out truthMode);
            DateTime now = DateTime.UtcNow;

            if (_manualConfirmationPending)
            {
                if (hasTruth && truthMode == Mode)
                {
                    _manualConfirmationPending = false;
                    _manualNextReconcileUtc = now.AddMilliseconds(ManualReconcileCooldownMs);
                    return;
                }

                if (now < _manualConfirmationDeadlineUtc)
                {
                    return;
                }

                _manualConfirmationPending = false;
                _manualNextReconcileUtc = now.AddMilliseconds(ManualReconcileCooldownMs);

                if (hasTruth && truthMode != Mode)
                {
                    ApplyManualTruthMode(truthMode, "manual-confirmation-fallback");
                }

                return;
            }

            if (now < _manualNextReconcileUtc)
            {
                if (hasTruth && truthMode != Mode)
                {
                    ApplyManualTruthMode(truthMode, "manual-external-truth-sync");
                }
                return;
            }

            _manualNextReconcileUtc = now.AddMilliseconds(ManualReconcileCooldownMs);
            if (hasTruth && truthMode != Mode)
            {
                ApplyManualTruthMode(truthMode, "manual-external-truth-sync");
            }
        }

        private void ApplyManualTruthMode(PitTyreControlMode truthMode, string reason)
        {
            Mode = truthMode;
            ResetAttemptState();
            _manualConfirmationPending = false;
            _manualConfirmationDeadlineUtc = DateTime.MinValue;
            _manualNextReconcileUtc = DateTime.UtcNow.AddMilliseconds(ManualReconcileCooldownMs);

            _logger?.Invoke($"[LalaPlugin:PitTyreControl] Manual truth sync remap reason={reason} mode={ModeToText(truthMode)}");
        }

        private static bool TryMapManualTruthMode(PitTyreControlSnapshot snapshot, out PitTyreControlMode truthMode)
        {
            truthMode = PitTyreControlMode.Off;

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

        private void HandleAutoFailureFeedback(PitTyreControlSnapshot snapshot, bool desiredService, bool desiredWet)
        {
            if (desiredService && snapshot.IsTireServiceSelected)
            {
                _autoServiceFailureNoticeKey = string.Empty;
            }

            string serviceTargetKey = desiredService ? "on" : "off";
            if (desiredService &&
                !snapshot.IsTireServiceSelected &&
                _serviceTargetAttempts >= MaxAttemptsPerTarget &&
                !string.Equals(_autoServiceFailureNoticeKey, serviceTargetKey, StringComparison.Ordinal))
            {
                _autoServiceFailureNoticeKey = serviceTargetKey;
                PublishSelectionFeedback("Pit.TyreControl.Auto.ServiceUnconfirmed", "TYRE AUTO UNCONFIRMED");
                _logger?.Invoke("[LalaPlugin:PitTyreControl] AUTO enforcement unconfirmed: tyre service stayed OFF after bounded attempts.");
            }

            bool compoundConfirmed = snapshot.HasRequestedCompound &&
                                     IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, desiredWet);
            string compoundTargetKey = desiredWet ? "wet" : "dry";
            if (compoundConfirmed)
            {
                _autoCompoundFailureNoticeKey = string.Empty;
                return;
            }

            if (_compoundTargetAttempts >= MaxAttemptsPerTarget &&
                !string.Equals(_autoCompoundFailureNoticeKey, compoundTargetKey, StringComparison.Ordinal))
            {
                _autoCompoundFailureNoticeKey = compoundTargetKey;
                PublishSelectionFeedback("Pit.TyreControl.Auto.CompoundUnconfirmed", "TYRE AUTO UNCONFIRMED");
                _logger?.Invoke($"[LalaPlugin:PitTyreControl] AUTO enforcement unconfirmed: compound target={(desiredWet ? "WET" : "DRY")} not confirmed after bounded attempts.");
            }
        }

        private void EnsureTyreService(PitTyreControlSnapshot snapshot, bool desiredSelected)
        {
            string targetKey = desiredSelected ? "on" : "off";
            if (snapshot.IsTireServiceSelected == desiredSelected)
            {
                _lastServiceTargetKey = targetKey;
                _serviceTargetAttempts = 0;
                _serviceNextAttemptUtc = DateTime.MinValue;
                return;
            }

            if (!string.Equals(_lastServiceTargetKey, targetKey, StringComparison.Ordinal))
            {
                _lastServiceTargetKey = targetKey;
                _serviceTargetAttempts = 0;
                _serviceNextAttemptUtc = DateTime.MinValue;
            }

            if (!CanAttempt(_serviceTargetAttempts, _serviceNextAttemptUtc))
            {
                return;
            }

            _builtInActionSender?.Invoke(PitCommandAction.ToggleTyresAll);
            _serviceTargetAttempts++;
            _serviceNextAttemptUtc = DateTime.UtcNow.AddMilliseconds(SendCooldownMs);
        }

        private void EnsureCompound(PitTyreControlSnapshot snapshot, bool desiredWet)
        {
            string targetKey = desiredWet ? "wet" : "dry";
            bool alreadyCorrect = snapshot.HasRequestedCompound &&
                                  IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, desiredWet);
            if (alreadyCorrect)
            {
                _lastCompoundTargetKey = targetKey;
                _compoundTargetAttempts = 0;
                _compoundNextAttemptUtc = DateTime.MinValue;
                return;
            }

            if (!string.Equals(_lastCompoundTargetKey, targetKey, StringComparison.Ordinal))
            {
                _lastCompoundTargetKey = targetKey;
                _compoundTargetAttempts = 0;
                _compoundNextAttemptUtc = DateTime.MinValue;
            }

            if (!CanAttempt(_compoundTargetAttempts, _compoundNextAttemptUtc))
            {
                return;
            }

            string command = desiredWet ? "#tc 2$" : "#tc 0$";
            string actionName = desiredWet ? "Pit.TyreControl.SetWetCompound" : "Pit.TyreControl.SetDryCompound";
            string feedback = desiredWet ? "TYRE WET" : "TYRE DRY";
            LogCompoundAttempt(snapshot, desiredWet, command);
            _rawCommandSender?.Invoke(actionName, command, feedback);
            _compoundTargetAttempts++;
            _compoundNextAttemptUtc = DateTime.UtcNow.AddMilliseconds(SendCooldownMs);
        }

        private void LogCompoundAttempt(PitTyreControlSnapshot snapshot, bool desiredWet, string command)
        {
            string requested = snapshot.HasRequestedCompound ? snapshot.RequestedCompound.ToString() : "NA";
            string player = snapshot.HasPlayerCompound ? snapshot.PlayerCompound.ToString() : "NA";
            string wetFlag = snapshot.WeatherDeclaredWet ? "true" : "false";
            string desired = desiredWet ? "WET" : "DRY";
            _logger?.Invoke(
                $"[LalaPlugin:PitTyreControl] Compound change attempt target={desired} cmd='{command}' requested={requested} player={player} weatherDeclaredWet={wetFlag} available01='{snapshot.AvailableCompound01 ?? string.Empty}' available02='{snapshot.AvailableCompound02 ?? string.Empty}'");
        }

        private void PublishSelectionFeedback(string actionName, string message)
        {
            _feedbackPublisher?.Invoke(actionName, message, string.Empty);
        }

        private static bool IsRequestedCompoundInDesiredFamily(int requestedCompound, bool desiredWet)
        {
            if (desiredWet)
            {
                return requestedCompound == 2 || requestedCompound == 3;
            }

            return requestedCompound == 0 || requestedCompound == 1;
        }

        private static bool CanAttempt(int attemptCount, DateTime nextAttemptUtc)
        {
            if (attemptCount >= MaxAttemptsPerTarget)
            {
                return false;
            }

            return DateTime.UtcNow >= nextAttemptUtc;
        }

        private void ResetAttemptState()
        {
            _lastServiceTargetKey = string.Empty;
            _serviceTargetAttempts = 0;
            _serviceNextAttemptUtc = DateTime.MinValue;
            ResetCompoundAttempts();
        }

        private void ResetManualSyncState()
        {
            _manualConfirmationPending = false;
            _manualConfirmationDeadlineUtc = DateTime.MinValue;
            _manualNextReconcileUtc = DateTime.MinValue;
        }

        private void ResetAutoFailureNoticeState()
        {
            _autoServiceFailureNoticeKey = string.Empty;
            _autoCompoundFailureNoticeKey = string.Empty;
        }

        private void ResetCompoundAttempts()
        {
            _lastCompoundTargetKey = string.Empty;
            _compoundTargetAttempts = 0;
            _compoundNextAttemptUtc = DateTime.MinValue;
        }

        private static string ModeToText(PitTyreControlMode mode)
        {
            switch (mode)
            {
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
