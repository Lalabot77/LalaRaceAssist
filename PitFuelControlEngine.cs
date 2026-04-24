using System;

namespace LaunchPlugin
{
    internal enum PitFuelControlSource
    {
        Push = 0,
        Norm = 1,
        Save = 2,
        Plan = 3,
        Stby = 4
    }

    internal enum PitFuelControlMode
    {
        Off = 0,
        Man = 1,
        Auto = 2
    }

    internal sealed class PitFuelControlSnapshot
    {
        public bool SuppressFuelControl;
        public bool IracingAutoFuelEnabled;
        public bool TelemetryFuelFillEnabled;
        public string LiveCar = string.Empty;
        public string LiveTrack = string.Empty;
        public bool HasLiveBasis;
        public bool LiveBasisIsTimeLimited;
        public bool HasLiveRaceLength;
        public double LiveRaceLengthValue;

        public string PlannerCar = string.Empty;
        public string PlannerTrack = string.Empty;
        public bool HasPlannerBasis;
        public bool PlannerBasisIsTimeLimited;
        public bool HasPlannerRaceLength;
        public double PlannerRaceLengthValue;

        public double TargetPushLitres;
        public double TargetNormLitres;
        public double TargetSaveLitres;
        public double TargetPlanLitres;

        public double CurrentFuelLitres;
        public double TankSpaceLitres;
        public double StopsRequiredToEnd;
        public double TelemetryRequestedFuelLitres;
    }

    internal sealed class PitFuelControlEngine
    {
        private const double OverrideArmStopsThreshold = 1.10;
        private const double OverrideDisarmStopsThreshold = 1.05;
        private const int PluginSendSuppressionMs = 900;
        private const int MaxFuelOvershootLitres = 500;

        private readonly Func<PitFuelControlSnapshot> _snapshotProvider;
        private readonly Func<string, string, string, bool> _chatCommandSender;
        private readonly Action<string, string, string> _feedbackPublisher;

        private DateTime _suppressManualOverrideUntilUtc = DateTime.MinValue;
        private bool _maxOverrideArmed;
        private bool _hasObservedRequestedFuelLitres;
        private int _lastObservedRequestedFuelLitres = -1;
        private bool _hasObservedFuelFillEnabled;
        private bool _lastObservedFuelFillEnabled;
        private bool _hasPendingOwnedRequestedFuelLitres;
        private int _pendingOwnedRequestedFuelLitres = -1;
        private bool _hasPendingOwnedFuelFillEnabled;
        private bool _pendingOwnedFuelFillEnabled;

        public PitFuelControlSource Source { get; private set; } = PitFuelControlSource.Push;
        private bool IsAutoModeActive { get; set; }
        public PitFuelControlMode Mode => ResolveEffectiveMode();
        public bool AutoArmed { get; private set; }
        public int LastSentFuelLitres { get; private set; } = -1;
        public double TargetLitres { get; private set; }
        public bool OverrideActive { get; private set; }

        public bool PlanValid { get; private set; }

        public PitFuelControlEngine(
            Func<PitFuelControlSnapshot> snapshotProvider,
            Func<string, string, string, bool> chatCommandSender,
            Action<string, string, string> feedbackPublisher)
        {
            _snapshotProvider = snapshotProvider;
            _chatCommandSender = chatCommandSender;
            _feedbackPublisher = feedbackPublisher;
        }

        public void ResetToOffStby()
        {
            Source = PitFuelControlSource.Stby;
            IsAutoModeActive = false;
            AutoArmed = false;
            LastSentFuelLitres = -1;
            ClearObservedExternalState();
            TargetLitres = 0.0;
            OverrideActive = false;
            _maxOverrideArmed = false;
            _suppressManualOverrideUntilUtc = DateTime.MinValue;
            RefreshDerivedState();
        }

        public void SourceCycle()
        {
            if (TryApplySuppressedState())
            {
                return;
            }

            RefreshDerivedState();
            PitFuelControlMode effectiveMode = ResolveEffectiveMode();
            if (effectiveMode == PitFuelControlMode.Off)
            {
                Source = PitFuelControlSource.Stby;
                AutoArmed = false;
                IsAutoModeActive = false;
                return;
            }

            if (effectiveMode == PitFuelControlMode.Auto)
            {
                if (Source == PitFuelControlSource.Plan)
                {
                    Source = PitFuelControlSource.Stby;
                    AutoArmed = false;
                    return;
                }

                Source = NextAutoSource(Source);
            }
            else
            {
                if (Source == PitFuelControlSource.Stby)
                {
                    Source = PitFuelControlSource.Push;
                }
                else
                {
                    Source = NextSource(Source);
                }
            }

            RefreshDerivedState();

            bool sendAttempted = false;
            if (effectiveMode == PitFuelControlMode.Man || effectiveMode == PitFuelControlMode.Auto)
            {
                sendAttempted = true;
                bool sent = SendCurrentTarget(isAutoUpdate: false, actionNameOverride: "Pit.FuelControl.SourceCycle");
                if (!sent)
                {
                    Source = PitFuelControlSource.Stby;
                    if (effectiveMode == PitFuelControlMode.Auto)
                    {
                        AutoArmed = false;
                    }
                    PublishSelectionFeedback("Pit.FuelControl.SourceCycle", "Pit Cmd Fail");
                    return;
                }

                if (effectiveMode == PitFuelControlMode.Auto)
                {
                    AutoArmed = true;
                }
            }

            if (!sendAttempted)
            {
                PublishSelectionFeedback("Pit.FuelControl.SourceCycle", string.Format("FUEL SRC {0}", SourceToText(Source)));
            }
        }

        public void ModeCycle()
        {
            if (TryApplySuppressedState())
            {
                return;
            }

            RefreshDerivedState();
            PitFuelControlMode effectiveMode = ResolveEffectiveMode();
            if (effectiveMode == PitFuelControlMode.Off)
            {
                Source = PitFuelControlSource.Stby;
                bool sentOn = _chatCommandSender != null &&
                              _chatCommandSender("Pit.FuelControl.ModeCycle", "#fuel$", "FUEL MAN STBY");
                if (!sentOn)
                {
                    PublishSelectionFeedback("Pit.FuelControl.ModeCycle", "Pit Cmd Fail");
                    return;
                }

                IsAutoModeActive = false;
                AutoArmed = false;
                QueueOwnedMirrorExpectation(null, true);
                return;
            }

            if (effectiveMode == PitFuelControlMode.Man)
            {
                IsAutoModeActive = true;
                if (Source == PitFuelControlSource.Stby)
                {
                    AutoArmed = false;
                    PublishSelectionFeedback("Pit.FuelControl.ModeCycle", "AUTO REFUEL STBY");
                    return;
                }

                if (Source == PitFuelControlSource.Plan)
                {
                    Source = PitFuelControlSource.Stby;
                    AutoArmed = false;
                    PublishSelectionFeedback("Pit.FuelControl.ModeCycle", "AUTO REFUEL STBY");
                    return;
                }

                bool sent = SendCurrentTarget(isAutoUpdate: false, actionNameOverride: "Pit.FuelControl.ModeCycle");
                if (sent)
                {
                    AutoArmed = true;
                }
                else
                {
                    Source = PitFuelControlSource.Stby;
                    AutoArmed = false;
                    PublishSelectionFeedback("Pit.FuelControl.ModeCycle", "Pit Cmd Fail");
                }

                return;
            }

            if (Source == PitFuelControlSource.Plan)
            {
                Source = PitFuelControlSource.Stby;
                AutoArmed = false;
                return;
            }

            bool sentOff = _chatCommandSender != null &&
                           _chatCommandSender("Pit.FuelControl.ModeCycle", "#-fuel$", "REFUEL OFF");
            if (!sentOff)
            {
                PublishSelectionFeedback("Pit.FuelControl.ModeCycle", "Pit Cmd Fail");
                return;
            }

            IsAutoModeActive = false;
            AutoArmed = false;
            Source = PitFuelControlSource.Stby;
            LastSentFuelLitres = -1;
            _suppressManualOverrideUntilUtc = DateTime.MinValue;
            ClearObservedExternalState();
            QueueOwnedMirrorExpectation(null, false);
        }

        public void SetPush() => SetSource(PitFuelControlSource.Push, "Pit.FuelControl.SetPush");
        public void SetNorm() => SetSource(PitFuelControlSource.Norm, "Pit.FuelControl.SetNorm");
        public void SetSave() => SetSource(PitFuelControlSource.Save, "Pit.FuelControl.SetSave");
        public void SetPlan() => SetPlanSource("Pit.FuelControl.SetPlan");

        public void OnLapCross()
        {
            if (TryApplySuppressedState())
            {
                return;
            }

            RefreshDerivedState();

            if (!IsAutoModeActive || !AutoArmed)
            {
                return;
            }

            if (Source == PitFuelControlSource.Plan || Source == PitFuelControlSource.Stby)
            {
                return;
            }

            int targetRounded = ComputeRoundedTargetLitres();
            if (targetRounded < 0)
            {
                return;
            }

            if (LastSentFuelLitres >= 0)
            {
                int delta = Math.Abs(targetRounded - LastSentFuelLitres);
                if (delta < 1 || targetRounded == LastSentFuelLitres)
                {
                    return;
                }
            }

            SendCurrentTarget(isAutoUpdate: true);
        }

        public void OnTelemetryTick()
        {
            RefreshDerivedState();
            var snapshot = _snapshotProvider();

            if (snapshot == null)
            {
                return;
            }

            if (DateTime.UtcNow < _suppressManualOverrideUntilUtc)
            {
                UpdateObservedExternalState(snapshot);
                return;
            }

            if (snapshot.SuppressFuelControl)
            {
                DisarmAutoAndForceStby(clearSuppressWindow: true);
                return;
            }

            int currentRequestedLitres = RoundUpLitres(snapshot.TelemetryRequestedFuelLitres);
            bool currentFuelFillEnabled = snapshot.TelemetryFuelFillEnabled;
            ExpireSatisfiedOwnedMirrorExpectations(currentRequestedLitres, currentFuelFillEnabled);
            if (!_hasObservedRequestedFuelLitres || !_hasObservedFuelFillEnabled)
            {
                _hasObservedRequestedFuelLitres = true;
                _lastObservedRequestedFuelLitres = currentRequestedLitres;
                _hasObservedFuelFillEnabled = true;
                _lastObservedFuelFillEnabled = currentFuelFillEnabled;
                return;
            }

            bool requestedFuelChanged = currentRequestedLitres != _lastObservedRequestedFuelLitres;
            bool fuelFillChanged = currentFuelFillEnabled != _lastObservedFuelFillEnabled;
            _lastObservedRequestedFuelLitres = currentRequestedLitres;
            _lastObservedFuelFillEnabled = currentFuelFillEnabled;

            if (snapshot.IracingAutoFuelEnabled)
            {
                CancelAutoToStby("Pit.FuelControl.AutoFuelOwnership", "AUTO CANCELLED");
                return;
            }

            if (requestedFuelChanged || fuelFillChanged)
            {
                if (TryConsumeOwnedMirrorChange(currentRequestedLitres, currentFuelFillEnabled, requestedFuelChanged, fuelFillChanged))
                {
                    return;
                }

                HandleExternalMirrorChange(currentFuelFillEnabled, requestedFuelChanged);
                return;
            }
        }

        public string SourceText => SourceToText(Source);
        public string ModeText => ModeToText(Mode);

        private void SetSource(PitFuelControlSource requestedSource, string actionName)
        {
            if (TryApplySuppressedState())
            {
                return;
            }

            RefreshDerivedState();
            PitFuelControlMode effectiveMode = ResolveEffectiveMode();
            if (effectiveMode == PitFuelControlMode.Off)
            {
                Source = PitFuelControlSource.Stby;
                AutoArmed = false;
                IsAutoModeActive = false;
                return;
            }

            if (requestedSource == PitFuelControlSource.Plan)
            {
                return;
            }

            Source = requestedSource;
            RefreshDerivedState();

            bool sendAttempted = false;
            if (ShouldSendOnManualOrAuto())
            {
                sendAttempted = true;
                bool sent = SendCurrentTarget(isAutoUpdate: false, actionNameOverride: actionName);
                if (!sent && effectiveMode == PitFuelControlMode.Man)
                {
                    Source = PitFuelControlSource.Stby;
                    PublishSelectionFeedback(actionName, "Pit Cmd Fail");
                    return;
                }
            }

            if (!sendAttempted)
            {
                PublishSelectionFeedback(actionName, string.Format("FUEL SRC {0}", SourceToText(Source)));
            }
        }

        private void SetPlanSource(string actionName)
        {
            if (TryApplySuppressedState())
            {
                return;
            }

            RefreshDerivedState();
            PitFuelControlMode effectiveMode = ResolveEffectiveMode();
            if (effectiveMode == PitFuelControlMode.Off)
            {
                Source = PitFuelControlSource.Stby;
                AutoArmed = false;
                IsAutoModeActive = false;
                return;
            }

            if (effectiveMode == PitFuelControlMode.Auto)
            {
                return;
            }

            if (!PlanValid)
            {
                if (effectiveMode == PitFuelControlMode.Man)
                {
                    PublishSelectionFeedback(actionName, "Pit Cmd Fail");
                }

                return;
            }

            Source = PitFuelControlSource.Plan;
            RefreshDerivedState();
            bool sent = SendCurrentTarget(isAutoUpdate: false, actionNameOverride: actionName);
            if (!sent)
            {
                Source = PitFuelControlSource.Stby;
                PublishSelectionFeedback(actionName, "Pit Cmd Fail");
            }
        }

        private bool SendCurrentTarget(bool isAutoUpdate, string actionNameOverride = null, bool allowOffMode = false)
        {
            RefreshDerivedState();
            if (!allowOffMode && ResolveEffectiveMode() == PitFuelControlMode.Off)
            {
                return false;
            }

            if (Source == PitFuelControlSource.Stby)
            {
                return false;
            }

            int roundedTarget = ComputeRoundedTargetLitres();
            if (roundedTarget < 0)
            {
                return false;
            }

            if (Source == PitFuelControlSource.Plan && !PlanValid)
            {
                return false;
            }

            var snapshot = _snapshotProvider();
            bool useMax = OverrideActive;
            bool showMaxFeedback = IsMaxStyleFeedbackRequest(roundedTarget, snapshot, useMax);
            string commandText = useMax
                ? string.Format("#fuel +{0}$", MaxFuelOvershootLitres)
                : string.Format("#fuel {0}$", roundedTarget);
            PitFuelControlMode feedbackMode = ResolveEffectiveMode();
            string feedback = BuildFeedbackText(isAutoUpdate, showMaxFeedback, roundedTarget, feedbackMode);
            string actionName = !string.IsNullOrWhiteSpace(actionNameOverride)
                ? actionNameOverride.Trim()
                : (isAutoUpdate ? "Pit.FuelControl.AutoUpdate" : "Pit.FuelControl.SourceSet");

            bool sent = _chatCommandSender != null && _chatCommandSender(actionName, commandText, feedback);
            if (sent)
            {
                LastSentFuelLitres = roundedTarget;
                _suppressManualOverrideUntilUtc = DateTime.UtcNow.AddMilliseconds(PluginSendSuppressionMs);
                UpdateObservedExternalState(snapshot);
                QueueOwnedMirrorExpectation(roundedTarget, null);
                if (IsAutoModeActive)
                {
                    AutoArmed = true;
                }
            }

            return sent;
        }

        private void PublishSelectionFeedback(string actionName, string message)
        {
            _feedbackPublisher?.Invoke(actionName, message, string.Empty);
        }

        private int ComputeRoundedTargetLitres()
        {
            var snapshot = _snapshotProvider();
            if (snapshot == null)
            {
                TargetLitres = 0.0;
                OverrideActive = false;
                return -1;
            }

            double rawTarget = ResolveSourceTarget(snapshot, Source);
            TargetLitres = rawTarget;

            bool canOverride = Source == PitFuelControlSource.Push ||
                               Source == PitFuelControlSource.Norm ||
                               Source == PitFuelControlSource.Save;

            if (canOverride)
            {
                if (_maxOverrideArmed)
                {
                    if (snapshot.StopsRequiredToEnd < OverrideDisarmStopsThreshold)
                    {
                        _maxOverrideArmed = false;
                    }
                }
                else if (snapshot.StopsRequiredToEnd > OverrideArmStopsThreshold)
                {
                    _maxOverrideArmed = true;
                }
            }
            else
            {
                _maxOverrideArmed = false;
            }

            OverrideActive = canOverride && _maxOverrideArmed;

            double effectiveTarget = OverrideActive
                ? Math.Max(0.0, snapshot.TankSpaceLitres)
                : Math.Max(0.0, rawTarget);

            return RoundUpLitres(effectiveTarget);
        }

        private static int RoundUpLitres(double litres)
        {
            if (double.IsNaN(litres) || litres < 0.0)
            {
                return 0;
            }

            return (int)Math.Ceiling(litres);
        }

        private static bool IsMaxStyleFeedbackRequest(int requestedLitres, PitFuelControlSnapshot snapshot, bool useMaxOverride)
        {
            if (useMaxOverride)
            {
                return true;
            }

            if (snapshot == null)
            {
                return false;
            }

            if (double.IsNaN(snapshot.TankSpaceLitres) || double.IsInfinity(snapshot.TankSpaceLitres))
            {
                return false;
            }

            if (snapshot.TankSpaceLitres < 0.0)
            {
                return false;
            }

            int requestedSafe = Math.Max(0, requestedLitres);
            return requestedSafe > snapshot.TankSpaceLitres;
        }

        private void RefreshDerivedState()
        {
            var snapshot = _snapshotProvider();
            if (snapshot == null)
            {
                PlanValid = false;
                TargetLitres = 0.0;
                OverrideActive = false;
                return;
            }

            if (snapshot.SuppressFuelControl)
            {
                DisarmAutoAndForceStby(clearSuppressWindow: true);
                PlanValid = false;
                TargetLitres = 0.0;
                OverrideActive = false;
                return;
            }

            PlanValid = ComputePlanValidity(snapshot);

            if (Source == PitFuelControlSource.Plan)
            {
                OverrideActive = false;
                _maxOverrideArmed = false;
            }

            TargetLitres = ResolveSourceTarget(snapshot, Source);
        }

        private bool TryApplySuppressedState()
        {
            var snapshot = _snapshotProvider();
            if (snapshot == null || !snapshot.SuppressFuelControl)
            {
                return false;
            }

            DisarmAutoAndForceStby(clearSuppressWindow: true);
            PlanValid = false;
            TargetLitres = 0.0;
            OverrideActive = false;
            return true;
        }

        private void DisarmAutoAndForceStby(bool clearSuppressWindow)
        {
            IsAutoModeActive = false;
            Source = PitFuelControlSource.Stby;
            AutoArmed = false;
            LastSentFuelLitres = -1;
            OverrideActive = false;
            _maxOverrideArmed = false;
            ClearObservedExternalState();
            if (clearSuppressWindow)
            {
                _suppressManualOverrideUntilUtc = DateTime.MinValue;
            }
        }

        private void CancelAutoToStby(string actionName, string message)
        {
            if (!IsAutoModeActive || !AutoArmed)
            {
                DisarmAutoAndForceStby(clearSuppressWindow: true);
                return;
            }

            DisarmAutoAndForceStby(clearSuppressWindow: true);
            PublishSelectionFeedback(actionName, message);
        }

        private void HandleExternalMirrorChange(bool currentFuelFillEnabled, bool requestedFuelChanged)
        {
            if (IsAutoModeActive)
            {
                DisarmAutoAndForceStby(clearSuppressWindow: true);
                PublishSelectionFeedback("Pit.FuelControl.ExternalMirror", "AUTO REFUEL CANCELLED BY MFD");
                return;
            }

            IsAutoModeActive = false;
            AutoArmed = false;
            Source = PitFuelControlSource.Stby;
            if (!currentFuelFillEnabled)
            {
                PublishSelectionFeedback("Pit.FuelControl.ExternalMirror", "REFUEL SET OFF BY MFD");
                return;
            }

            if (requestedFuelChanged)
            {
                PublishSelectionFeedback("Pit.FuelControl.ExternalMirror", "FUEL CHANGED BY MFD");
                return;
            }

            PublishSelectionFeedback("Pit.FuelControl.ExternalMirror", "REFUEL SET ON BY MFD");
        }

        private void UpdateObservedExternalState(PitFuelControlSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return;
            }

            _hasObservedRequestedFuelLitres = true;
            _lastObservedRequestedFuelLitres = RoundUpLitres(snapshot.TelemetryRequestedFuelLitres);
            _hasObservedFuelFillEnabled = true;
            _lastObservedFuelFillEnabled = snapshot.TelemetryFuelFillEnabled;
            ExpireSatisfiedOwnedMirrorExpectations(_lastObservedRequestedFuelLitres, _lastObservedFuelFillEnabled);
        }

        private void ClearObservedExternalState()
        {
            _hasObservedRequestedFuelLitres = false;
            _lastObservedRequestedFuelLitres = -1;
            _hasObservedFuelFillEnabled = false;
            _lastObservedFuelFillEnabled = false;
            _hasPendingOwnedRequestedFuelLitres = false;
            _pendingOwnedRequestedFuelLitres = -1;
            _hasPendingOwnedFuelFillEnabled = false;
            _pendingOwnedFuelFillEnabled = false;
        }

        private void QueueOwnedMirrorExpectation(int? requestedFuelLitres, bool? fuelFillEnabled)
        {
            if (requestedFuelLitres.HasValue)
            {
                _hasPendingOwnedRequestedFuelLitres = true;
                _pendingOwnedRequestedFuelLitres = Math.Max(0, requestedFuelLitres.Value);
            }

            if (fuelFillEnabled.HasValue)
            {
                _hasPendingOwnedFuelFillEnabled = true;
                _pendingOwnedFuelFillEnabled = fuelFillEnabled.Value;
            }
        }

        private void ExpireSatisfiedOwnedMirrorExpectations(int currentRequestedLitres, bool currentFuelFillEnabled)
        {
            if (_hasPendingOwnedRequestedFuelLitres && currentRequestedLitres == _pendingOwnedRequestedFuelLitres)
            {
                _hasPendingOwnedRequestedFuelLitres = false;
                _pendingOwnedRequestedFuelLitres = -1;
            }

            if (_hasPendingOwnedFuelFillEnabled && currentFuelFillEnabled == _pendingOwnedFuelFillEnabled)
            {
                _hasPendingOwnedFuelFillEnabled = false;
                _pendingOwnedFuelFillEnabled = false;
            }
        }

        private bool TryConsumeOwnedMirrorChange(int currentRequestedLitres, bool currentFuelFillEnabled, bool requestedFuelChanged, bool fuelFillChanged)
        {
            bool requestedOwned = !requestedFuelChanged;
            bool fuelFillOwned = !fuelFillChanged;

            if (requestedFuelChanged && _hasPendingOwnedRequestedFuelLitres && currentRequestedLitres == _pendingOwnedRequestedFuelLitres)
            {
                requestedOwned = true;
                _hasPendingOwnedRequestedFuelLitres = false;
                _pendingOwnedRequestedFuelLitres = -1;
            }

            if (fuelFillChanged && _hasPendingOwnedFuelFillEnabled && currentFuelFillEnabled == _pendingOwnedFuelFillEnabled)
            {
                fuelFillOwned = true;
                _hasPendingOwnedFuelFillEnabled = false;
                _pendingOwnedFuelFillEnabled = false;
            }

            return requestedOwned && fuelFillOwned;
        }

        private PitFuelControlMode ResolveEffectiveMode()
        {
            if (IsAutoModeActive)
            {
                return PitFuelControlMode.Auto;
            }

            var snapshot = _snapshotProvider();
            bool fuelEnabled = snapshot != null && snapshot.TelemetryFuelFillEnabled;
            return fuelEnabled ? PitFuelControlMode.Man : PitFuelControlMode.Off;
        }

        private bool ShouldSendOnManualOrAuto()
        {
            PitFuelControlMode effectiveMode = ResolveEffectiveMode();
            return IsAutoModeActive || effectiveMode == PitFuelControlMode.Man;
        }

        private static bool ComputePlanValidity(PitFuelControlSnapshot snapshot)
        {
            var match = PlannerLiveSessionMatchHelper.Evaluate(new PlannerLiveSessionMatchSnapshot
            {
                LiveCar = snapshot?.LiveCar,
                LiveTrack = snapshot?.LiveTrack,
                HasLiveBasis = snapshot != null && snapshot.HasLiveBasis,
                LiveBasisIsTimeLimited = snapshot != null && snapshot.LiveBasisIsTimeLimited,
                HasLiveRaceLength = snapshot != null && snapshot.HasLiveRaceLength,
                LiveRaceLengthValue = snapshot?.LiveRaceLengthValue ?? 0.0,
                PlannerCar = snapshot?.PlannerCar,
                PlannerTrack = snapshot?.PlannerTrack,
                HasPlannerBasis = snapshot != null && snapshot.HasPlannerBasis,
                PlannerBasisIsTimeLimited = snapshot != null && snapshot.PlannerBasisIsTimeLimited,
                HasPlannerRaceLength = snapshot != null && snapshot.HasPlannerRaceLength,
                PlannerRaceLengthValue = snapshot?.PlannerRaceLengthValue ?? 0.0
            });

            return match.IsMatch;
        }

        private static double ResolveSourceTarget(PitFuelControlSnapshot snapshot, PitFuelControlSource source)
        {
            switch (source)
            {
                case PitFuelControlSource.Push:
                    return Math.Max(0.0, snapshot.TargetPushLitres);
                case PitFuelControlSource.Norm:
                    return Math.Max(0.0, snapshot.TargetNormLitres);
                case PitFuelControlSource.Save:
                    return Math.Max(0.0, snapshot.TargetSaveLitres);
                case PitFuelControlSource.Plan:
                    return Math.Max(0.0, snapshot.TargetPlanLitres);
                default:
                    return 0.0;
            }
        }

        private string BuildFeedbackText(bool isAutoUpdate, bool showMaxFeedback, int roundedTarget, PitFuelControlMode feedbackMode)
        {
            string sourceText = SourceToText(Source);
            bool autoStyleFeedback = feedbackMode == PitFuelControlMode.Auto;

            if (isAutoUpdate)
            {
                return showMaxFeedback
                    ? string.Format("AUTO FUEL {0}L >MAX", roundedTarget)
                    : string.Format("AUTO FUEL {0}L", roundedTarget);
            }

            if (showMaxFeedback)
            {
                return autoStyleFeedback
                    ? string.Format("AUTO FUEL {0}L >MAX", roundedTarget)
                    : "FUEL MAX";
            }

            return autoStyleFeedback
                ? string.Format("AUTO REFUEL SET {0} {1}L", sourceText, roundedTarget)
                : string.Format("REFUEL SET {0} {1}L", sourceText, roundedTarget);
        }

        private static PitFuelControlSource NextSource(PitFuelControlSource current)
        {
            switch (current)
            {
                case PitFuelControlSource.Push:
                    return PitFuelControlSource.Norm;
                case PitFuelControlSource.Norm:
                    return PitFuelControlSource.Save;
                case PitFuelControlSource.Save:
                    return PitFuelControlSource.Plan;
                case PitFuelControlSource.Plan:
                    return PitFuelControlSource.Push;
                case PitFuelControlSource.Stby:
                default:
                    return PitFuelControlSource.Push;
            }
        }

        private static PitFuelControlSource NextAutoSource(PitFuelControlSource current)
        {
            switch (current)
            {
                case PitFuelControlSource.Push:
                    return PitFuelControlSource.Norm;
                case PitFuelControlSource.Norm:
                    return PitFuelControlSource.Save;
                case PitFuelControlSource.Save:
                case PitFuelControlSource.Plan:
                case PitFuelControlSource.Stby:
                default:
                    return PitFuelControlSource.Push;
            }
        }

        private static string SourceToText(PitFuelControlSource source)
        {
            switch (source)
            {
                case PitFuelControlSource.Push: return "PUSH";
                case PitFuelControlSource.Norm: return "NORM";
                case PitFuelControlSource.Save: return "SAVE";
                case PitFuelControlSource.Plan: return "PLAN";
                case PitFuelControlSource.Stby: return "STBY";
                default: return "PUSH";
            }
        }

        private static string ModeToText(PitFuelControlMode mode)
        {
            switch (mode)
            {
                case PitFuelControlMode.Off: return "OFF";
                case PitFuelControlMode.Man: return "MAN";
                case PitFuelControlMode.Auto: return "AUTO";
                default: return "OFF";
            }
        }
    }
}
