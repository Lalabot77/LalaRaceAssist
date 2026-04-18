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
        public string LiveCar = string.Empty;
        public string LiveTrack = string.Empty;
        public bool HasLiveBasis;
        public bool LiveBasisIsTimeLimited;

        public string PlannerCar = string.Empty;
        public string PlannerTrack = string.Empty;
        public bool HasPlannerBasis;
        public bool PlannerBasisIsTimeLimited;

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

        private readonly Func<PitFuelControlSnapshot> _snapshotProvider;
        private readonly Func<string, string, string, bool> _customCommandSender;
        private readonly Action<string> _feedbackPublisher;

        private DateTime _suppressManualOverrideUntilUtc = DateTime.MinValue;
        private bool _maxOverrideArmed;

        public PitFuelControlSource Source { get; private set; } = PitFuelControlSource.Push;
        public PitFuelControlMode Mode { get; private set; } = PitFuelControlMode.Off;
        public bool AutoArmed { get; private set; }
        public int LastSentFuelLitres { get; private set; } = -1;
        public double TargetLitres { get; private set; }
        public bool OverrideActive { get; private set; }

        public bool PlanValid { get; private set; }

        public PitFuelControlEngine(
            Func<PitFuelControlSnapshot> snapshotProvider,
            Func<string, string, string, bool> customCommandSender,
            Action<string> feedbackPublisher)
        {
            _snapshotProvider = snapshotProvider;
            _customCommandSender = customCommandSender;
            _feedbackPublisher = feedbackPublisher;
        }

        public void ResetForSession()
        {
            Source = PitFuelControlSource.Push;
            Mode = PitFuelControlMode.Off;
            AutoArmed = false;
            LastSentFuelLitres = -1;
            TargetLitres = 0.0;
            OverrideActive = false;
            _maxOverrideArmed = false;
            _suppressManualOverrideUntilUtc = DateTime.MinValue;
            RefreshDerivedState();
        }

        public void SourceCycle()
        {
            RefreshDerivedState();

            if (Source == PitFuelControlSource.Stby)
            {
                Source = PitFuelControlSource.Push;
            }
            else
            {
                Source = NextSource(Source, PlanValid);
            }

            RefreshDerivedState();

            if (Source == PitFuelControlSource.Plan && Mode == PitFuelControlMode.Auto)
            {
                Mode = PitFuelControlMode.Man;
                AutoArmed = false;
            }

            if (Mode == PitFuelControlMode.Man || Mode == PitFuelControlMode.Auto)
            {
                SendCurrentTarget(isAutoUpdate: false);
            }
        }

        public void ModeCycle()
        {
            RefreshDerivedState();

            if (Mode == PitFuelControlMode.Off)
            {
                Mode = PitFuelControlMode.Man;
                AutoArmed = false;
                return;
            }

            if (Mode == PitFuelControlMode.Man)
            {
                if (Source == PitFuelControlSource.Plan)
                {
                    Mode = PitFuelControlMode.Off;
                    AutoArmed = false;
                    return;
                }

                Mode = PitFuelControlMode.Auto;
                AutoArmed = true;
                return;
            }

            Mode = PitFuelControlMode.Off;
            AutoArmed = false;
        }

        public void OnLapCross()
        {
            RefreshDerivedState();

            if (Mode != PitFuelControlMode.Auto || !AutoArmed)
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

            if (Mode != PitFuelControlMode.Auto || !AutoArmed)
            {
                return;
            }

            if (DateTime.UtcNow < _suppressManualOverrideUntilUtc)
            {
                return;
            }

            var snapshot = _snapshotProvider();
            if (snapshot == null || LastSentFuelLitres < 0)
            {
                return;
            }

            int currentRequestedLitres = RoundUpLitres(snapshot.TelemetryRequestedFuelLitres);
            if (currentRequestedLitres < 0)
            {
                return;
            }

            if (Math.Abs(currentRequestedLitres - LastSentFuelLitres) >= 1)
            {
                Mode = PitFuelControlMode.Man;
                Source = PitFuelControlSource.Stby;
                AutoArmed = false;
                OverrideActive = false;
                _maxOverrideArmed = false;
                _feedbackPublisher?.Invoke("AUTO CANCELLED");
            }
        }

        public string SourceText => SourceToText(Source);
        public string ModeText => ModeToText(Mode);

        private void SendCurrentTarget(bool isAutoUpdate)
        {
            RefreshDerivedState();

            if (Source == PitFuelControlSource.Stby)
            {
                return;
            }

            int roundedTarget = ComputeRoundedTargetLitres();
            if (roundedTarget < 0)
            {
                return;
            }

            bool useMax = OverrideActive;
            string commandText = useMax ? "#fuel +150$" : string.Format("#fuel {0}$", roundedTarget);
            string feedback = BuildFeedbackText(isAutoUpdate, useMax, roundedTarget);
            string actionName = isAutoUpdate ? "Pit.FuelControl.AutoUpdate" : "Pit.FuelControl.SourceSet";

            bool sent = _customCommandSender != null && _customCommandSender(actionName, commandText, feedback);
            if (sent)
            {
                LastSentFuelLitres = roundedTarget;
                _suppressManualOverrideUntilUtc = DateTime.UtcNow.AddMilliseconds(PluginSendSuppressionMs);
                if (Mode == PitFuelControlMode.Auto)
                {
                    AutoArmed = true;
                }
            }
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

            PlanValid = ComputePlanValidity(snapshot);
            if (!PlanValid && Source == PitFuelControlSource.Plan)
            {
                Source = PitFuelControlSource.Push;
            }

            if (Source == PitFuelControlSource.Plan)
            {
                OverrideActive = false;
                _maxOverrideArmed = false;
            }

            TargetLitres = ResolveSourceTarget(snapshot, Source);
        }

        private static bool ComputePlanValidity(PitFuelControlSnapshot snapshot)
        {
            if (snapshot == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(snapshot.LiveCar) || string.IsNullOrWhiteSpace(snapshot.PlannerCar))
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(snapshot.LiveTrack) || string.IsNullOrWhiteSpace(snapshot.PlannerTrack))
            {
                return false;
            }

            if (!string.Equals(snapshot.LiveCar.Trim(), snapshot.PlannerCar.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!string.Equals(snapshot.LiveTrack.Trim(), snapshot.PlannerTrack.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (!snapshot.HasLiveBasis || !snapshot.HasPlannerBasis)
            {
                return false;
            }

            return snapshot.LiveBasisIsTimeLimited == snapshot.PlannerBasisIsTimeLimited;
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

        private string BuildFeedbackText(bool isAutoUpdate, bool useMax, int roundedTarget)
        {
            string sourceText = SourceToText(Source);
            if (isAutoUpdate)
            {
                if (useMax)
                {
                    return string.Format("AUTO FUEL MAX {0}L", roundedTarget);
                }

                return string.Format("AUTO FUEL UPDATE {0}L", roundedTarget);
            }

            if (useMax)
            {
                return string.Format("FUEL SET {0} MAX {1}L", sourceText, roundedTarget);
            }

            return string.Format("FUEL SET {0} {1}L", sourceText, roundedTarget);
        }

        private static PitFuelControlSource NextSource(PitFuelControlSource current, bool planValid)
        {
            switch (current)
            {
                case PitFuelControlSource.Push:
                    return PitFuelControlSource.Norm;
                case PitFuelControlSource.Norm:
                    return PitFuelControlSource.Save;
                case PitFuelControlSource.Save:
                    return planValid ? PitFuelControlSource.Plan : PitFuelControlSource.Push;
                case PitFuelControlSource.Plan:
                    return PitFuelControlSource.Push;
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
