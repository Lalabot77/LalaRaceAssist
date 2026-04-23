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
        private const int CommandConfirmationWindowMs = 900;
        private const int ManualConfirmationWindowMs = 1400;
        private const int ManualReconcileCooldownMs = 700;
        private const int PluginOwnedSuppressionWindowMs = 1200;
        private const int PluginOwnedIntentGraceMs = 4500;

        private readonly Func<PitTyreControlSnapshot> _snapshotProvider;
        private readonly Func<string, string, string, bool> _rawCommandSender;
        private readonly Action<string, string, string> _feedbackPublisher;
        private readonly Action<string> _logger;

        private string _lastServiceTargetKey = string.Empty;
        private bool _serviceConfirmationPending;
        private DateTime _serviceConfirmationDeadlineUtc = DateTime.MinValue;

        private string _lastCompoundTargetKey = string.Empty;
        private bool _compoundConfirmationPending;
        private DateTime _compoundConfirmationDeadlineUtc = DateTime.MinValue;

        private bool _manualConfirmationPending;
        private DateTime _manualConfirmationDeadlineUtc = DateTime.MinValue;
        private DateTime _manualNextReconcileUtc = DateTime.MinValue;

        private DateTime _pluginOwnedSuppressionUntilUtc = DateTime.MinValue;
        private bool _hasObservedTruthSample;
        private bool _observedHasServiceSelection;
        private bool _observedServiceSelected;
        private bool _observedHasRequestedCompound;
        private int _observedRequestedCompound = -1;
        private bool _hasPendingServiceIntent;
        private bool _pendingServiceSelected;
        private DateTime _pendingServiceIntentUntilUtc = DateTime.MinValue;
        private bool _hasPendingCompoundIntent;
        private bool _pendingCompoundWet;
        private DateTime _pendingCompoundIntentUntilUtc = DateTime.MinValue;

        public PitTyreControlMode Mode { get; private set; } = PitTyreControlMode.Off;

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
            ResetAttemptState();
            ResetManualSyncState();
            ResetPendingPluginIntentState();
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
                if (TryCancelAutoForExternalOwnership(snapshot))
                {
                    UpdateObservedTruthSample(snapshot);
                    return;
                }

                bool desiredWetAuto = snapshot.WeatherDeclaredWet;
                EnsureCompound(snapshot, desiredWetAuto);
                UpdateObservedTruthSample(snapshot);
                return;
            }

            TryReconcileManualModeToTruth(snapshot);

            if (Mode == PitTyreControlMode.Off)
            {
                EnsureTyreService(snapshot, false);
                ResetCompoundAttempts();
                UpdateObservedTruthSample(snapshot);
                return;
            }

            bool desiredWetMan = Mode == PitTyreControlMode.Wet;
            EnsureCompound(snapshot, desiredWetMan);
            UpdateObservedTruthSample(snapshot);
        }

        private void SetMode(PitTyreControlMode mode, string actionName)
        {
            Mode = mode;
            ResetAttemptState();
            ResetPendingPluginIntentState();
            BeginOrClearManualConfirmation(mode);
            PublishSelectionFeedback(actionName, string.Format("TYRE CHANGE {0}", ModeToText(mode)));
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

                HandleUnconfirmedCommand(snapshot, "manual-confirmation-fallback");

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

        private void EnsureTyreService(PitTyreControlSnapshot snapshot, bool desiredSelected)
        {
            if (!snapshot.HasTireServiceSelection)
            {
                return;
            }

            string targetKey = desiredSelected ? "ON" : "OFF";
            if (snapshot.IsTireServiceSelected == desiredSelected)
            {
                _lastServiceTargetKey = targetKey;
                _serviceConfirmationPending = false;
                _serviceConfirmationDeadlineUtc = DateTime.MinValue;
                ClearPendingServiceIntentIfMatched(snapshot);
                return;
            }

            bool targetChanged = !string.Equals(_lastServiceTargetKey, targetKey, StringComparison.Ordinal);
            if (targetChanged)
            {
                _serviceConfirmationPending = false;
                _serviceConfirmationDeadlineUtc = DateTime.MinValue;
            }

            if (desiredSelected)
            {
                return;
            }

            if (_serviceConfirmationPending)
            {
                if (DateTime.UtcNow < _serviceConfirmationDeadlineUtc)
                {
                    return;
                }

                _serviceConfirmationPending = false;
                _serviceConfirmationDeadlineUtc = DateTime.MinValue;
                HandleUnconfirmedCommand(snapshot, "service-confirmation-timeout");
                return;
            }

            if (!targetChanged && string.Equals(_lastServiceTargetKey, targetKey, StringComparison.Ordinal))
            {
                return;
            }

            bool sendAttempted = _rawCommandSender?.Invoke("Pit.TyreControl.ServiceOff", "#cleartires$", "TYRE OFF") ?? false;
            if (sendAttempted)
            {
                MarkPendingServiceIntent(desiredSelected);
                MarkPluginOwnedSuppressionWindow();
                _serviceConfirmationPending = true;
                _serviceConfirmationDeadlineUtc = DateTime.UtcNow.AddMilliseconds(CommandConfirmationWindowMs);
                _lastServiceTargetKey = targetKey;
                return;
            }

            _lastServiceTargetKey = targetKey;
            HandleUnconfirmedCommand(snapshot, "service-send-failed");
        }

        private void EnsureCompound(PitTyreControlSnapshot snapshot, bool desiredWet)
        {
            string targetKey = desiredWet ? "wet" : "dry";
            bool alreadyCorrect = snapshot.HasRequestedCompound &&
                                  IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, desiredWet);
            if (alreadyCorrect)
            {
                _lastCompoundTargetKey = targetKey;
                _compoundConfirmationPending = false;
                _compoundConfirmationDeadlineUtc = DateTime.MinValue;
                ClearPendingCompoundIntentIfMatched(snapshot);
                return;
            }

            bool targetChanged = !string.Equals(_lastCompoundTargetKey, targetKey, StringComparison.Ordinal);
            if (targetChanged)
            {
                _compoundConfirmationPending = false;
                _compoundConfirmationDeadlineUtc = DateTime.MinValue;
            }

            if (_compoundConfirmationPending)
            {
                if (DateTime.UtcNow < _compoundConfirmationDeadlineUtc)
                {
                    return;
                }

                _compoundConfirmationPending = false;
                _compoundConfirmationDeadlineUtc = DateTime.MinValue;
                HandleUnconfirmedCommand(snapshot, "compound-confirmation-timeout");
                return;
            }

            if (!targetChanged && string.Equals(_lastCompoundTargetKey, targetKey, StringComparison.Ordinal))
            {
                return;
            }

            string command = desiredWet ? "#tc 2$" : "#tc 0$";
            string actionName = desiredWet ? "Pit.TyreControl.SetWetCompound" : "Pit.TyreControl.SetDryCompound";
            string feedback = desiredWet ? "TYRE CHANGE WET" : "TYRE CHANGE DRY";
            LogCompoundAttempt(snapshot, desiredWet, command);
            bool sendAttempted = _rawCommandSender?.Invoke(actionName, command, feedback) ?? false;
            if (sendAttempted)
            {
                MarkPendingCompoundIntent(desiredWet);
                MarkPluginOwnedSuppressionWindow();
                _compoundConfirmationPending = true;
                _compoundConfirmationDeadlineUtc = DateTime.UtcNow.AddMilliseconds(CommandConfirmationWindowMs);
                _lastCompoundTargetKey = targetKey;
                return;
            }

            _lastCompoundTargetKey = targetKey;
            HandleUnconfirmedCommand(snapshot, "compound-send-failed");
        }

        private bool TryCancelAutoForExternalOwnership(PitTyreControlSnapshot snapshot)
        {
            if (!_hasObservedTruthSample)
            {
                return false;
            }

            if (!HasObservedTruthChanged(snapshot))
            {
                return false;
            }

            if (DateTime.UtcNow <= _pluginOwnedSuppressionUntilUtc)
            {
                return false;
            }

            if (IsObservedTruthConvergingToPluginIntent(snapshot))
            {
                return false;
            }

            PitTyreControlMode truthMode;
            if (!TryMapManualTruthMode(snapshot, out truthMode))
            {
                return false;
            }

            ApplyManualTruthMode(truthMode, "auto-cancel-external-ownership");
            PublishSelectionFeedback("Pit.TyreControl.Auto.CancelledExternal", "TYRE AUTO CANCELLED");
            _logger?.Invoke($"[LalaPlugin:PitTyreControl] AUTO cancelled: external tyre ownership detected, remapped={ModeToText(truthMode)}.");
            return true;
        }

        private bool HasObservedTruthChanged(PitTyreControlSnapshot snapshot)
        {
            if (_observedHasServiceSelection != snapshot.HasTireServiceSelection)
            {
                return true;
            }

            if (_observedHasServiceSelection && _observedServiceSelected != snapshot.IsTireServiceSelected)
            {
                return true;
            }

            if (_observedHasRequestedCompound != snapshot.HasRequestedCompound)
            {
                return true;
            }

            if (_observedHasRequestedCompound && _observedRequestedCompound != snapshot.RequestedCompound)
            {
                return true;
            }

            return false;
        }

        private void UpdateObservedTruthSample(PitTyreControlSnapshot snapshot)
        {
            _observedHasServiceSelection = snapshot.HasTireServiceSelection;
            _observedServiceSelected = snapshot.IsTireServiceSelected;
            _observedHasRequestedCompound = snapshot.HasRequestedCompound;
            _observedRequestedCompound = snapshot.RequestedCompound;
            _hasObservedTruthSample = true;
        }

        private void MarkPluginOwnedSuppressionWindow()
        {
            _pluginOwnedSuppressionUntilUtc = DateTime.UtcNow.AddMilliseconds(PluginOwnedSuppressionWindowMs);
        }

        private bool IsObservedTruthConvergingToPluginIntent(PitTyreControlSnapshot snapshot)
        {
            DateTime now = DateTime.UtcNow;
            bool serviceIntentRelevant = _hasPendingServiceIntent && now <= _pendingServiceIntentUntilUtc;
            bool compoundIntentRelevant = _hasPendingCompoundIntent && now <= _pendingCompoundIntentUntilUtc;

            bool serviceMatched = !serviceIntentRelevant;
            bool compoundMatched = !compoundIntentRelevant;

            if (serviceIntentRelevant)
            {
                if (snapshot.HasTireServiceSelection && snapshot.IsTireServiceSelected == _pendingServiceSelected)
                {
                    serviceMatched = true;
                    _hasPendingServiceIntent = false;
                    _pendingServiceIntentUntilUtc = DateTime.MinValue;
                }
            }
            else if (_hasPendingServiceIntent)
            {
                _hasPendingServiceIntent = false;
                _pendingServiceIntentUntilUtc = DateTime.MinValue;
            }

            if (compoundIntentRelevant)
            {
                if (snapshot.HasRequestedCompound &&
                    IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, _pendingCompoundWet))
                {
                    compoundMatched = true;
                    _hasPendingCompoundIntent = false;
                    _pendingCompoundIntentUntilUtc = DateTime.MinValue;
                }
            }
            else if (_hasPendingCompoundIntent)
            {
                _hasPendingCompoundIntent = false;
                _pendingCompoundIntentUntilUtc = DateTime.MinValue;
            }

            if (!serviceIntentRelevant && !compoundIntentRelevant)
            {
                return false;
            }

            return serviceMatched && compoundMatched;
        }

        private void MarkPendingServiceIntent(bool desiredSelected)
        {
            _hasPendingServiceIntent = true;
            _pendingServiceSelected = desiredSelected;
            _pendingServiceIntentUntilUtc = DateTime.UtcNow.AddMilliseconds(PluginOwnedIntentGraceMs);
        }

        private void MarkPendingCompoundIntent(bool desiredWet)
        {
            _hasPendingCompoundIntent = true;
            _pendingCompoundWet = desiredWet;
            _pendingCompoundIntentUntilUtc = DateTime.UtcNow.AddMilliseconds(PluginOwnedIntentGraceMs);
        }

        private void ClearPendingServiceIntentIfMatched(PitTyreControlSnapshot snapshot)
        {
            if (_hasPendingServiceIntent &&
                snapshot.HasTireServiceSelection &&
                snapshot.IsTireServiceSelected == _pendingServiceSelected)
            {
                _hasPendingServiceIntent = false;
                _pendingServiceIntentUntilUtc = DateTime.MinValue;
            }
        }

        private void ClearPendingCompoundIntentIfMatched(PitTyreControlSnapshot snapshot)
        {
            if (_hasPendingCompoundIntent &&
                snapshot.HasRequestedCompound &&
                IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, _pendingCompoundWet))
            {
                _hasPendingCompoundIntent = false;
                _pendingCompoundIntentUntilUtc = DateTime.MinValue;
            }
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

        private void HandleUnconfirmedCommand(PitTyreControlSnapshot snapshot, string reason)
        {
            PublishSelectionFeedback("Pit.TyreControl.Unconfirmed", "PIT CMD FAIL");

            PitTyreControlMode truthMode;
            if (TryMapManualTruthMode(snapshot, out truthMode))
            {
                if (truthMode != Mode)
                {
                    ApplyManualTruthMode(truthMode, reason);
                }
                else
                {
                    _logger?.Invoke($"[LalaPlugin:PitTyreControl] Unconfirmed command reason={reason}; mode already aligned with MFD truth={ModeToText(truthMode)}.");
                }

                return;
            }

            _logger?.Invoke($"[LalaPlugin:PitTyreControl] Unconfirmed command reason={reason}; unable to remap (MFD truth unavailable).");
        }

        private void ResetAttemptState()
        {
            _lastServiceTargetKey = string.Empty;
            _serviceConfirmationPending = false;
            _serviceConfirmationDeadlineUtc = DateTime.MinValue;
            ResetCompoundAttempts();
        }

        private void ResetManualSyncState()
        {
            _manualConfirmationPending = false;
            _manualConfirmationDeadlineUtc = DateTime.MinValue;
            _manualNextReconcileUtc = DateTime.MinValue;
        }

        private void ResetPendingPluginIntentState()
        {
            _hasPendingServiceIntent = false;
            _pendingServiceSelected = false;
            _pendingServiceIntentUntilUtc = DateTime.MinValue;
            _hasPendingCompoundIntent = false;
            _pendingCompoundWet = false;
            _pendingCompoundIntentUntilUtc = DateTime.MinValue;
        }

        private void ResetCompoundAttempts()
        {
            _lastCompoundTargetKey = string.Empty;
            _compoundConfirmationPending = false;
            _compoundConfirmationDeadlineUtc = DateTime.MinValue;
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
