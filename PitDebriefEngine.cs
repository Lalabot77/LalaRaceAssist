using System;
using System.Globalization;
using System.Text;

namespace LaunchPlugin
{
    internal sealed class PitDebriefEngine
    {
        private const string Unknown = "UNKNOWN";
        private const string EmptyState = "EMPTY";
        private const string CollectingState = "COLLECTING";
        private const string LaneExitState = "LANE_EXIT";
        private const string AwaitingOutLapState = "AWAITING_OUT_LAP";
        private const string FinalState = "FINAL";

        private bool _collecting;
        private bool _finalLogPending;
        private DateTime _finalizedUtc = DateTime.MinValue;
        private string _finalLogLine = string.Empty;

        private bool _hasEntryLoss;
        private bool _hasPredictedTotalLoss;
        private bool _hasActualTotalLoss;
        private bool _hasLossDelta;
        private bool _hasFuelTarget;
        private bool _hasFuelAdded;
        private bool _hasRefuelDuration;
        private bool _hasRefuelRate;
        private bool _hasTyreCount;
        private bool _hasServiceSec;
        private bool _hasBoxDelta;
        private bool _hasRawBoxDelta;
        private bool _boxDeltaSuppressed;
        private bool _serviceKnown;
        private bool _boxOutcomeResolved;
        private bool _summaryFinalized;
        private bool _hasPredictedPosition;
        private bool _hasActualPosition;
        private bool _hasPositionDelta;
        private bool _boxSeen;
        private bool _missedBoxObserved;
        private PitPhase _missedBoxPhase = PitPhase.None;
        private string _boxRepairInfluenceText = string.Empty;

        public bool Valid { get; private set; }
        public int StopIndex { get; private set; }
        public string StateText { get; private set; } = EmptyState;
        public string SummaryText { get; private set; } = string.Empty;

        public string EntryQualityText { get; private set; } = Unknown;
        public double EntryLineTimeLossSec { get; private set; }
        public string EntryDecelQualityText { get; private set; } = Unknown;
        public string EntryLimiterQualityText { get; private set; } = Unknown;

        public string BoxQualityText { get; private set; } = Unknown;
        public string BoxMissedReason { get; private set; } = "unknown";
        public double BoxStationarySec { get; private set; }
        public double BoxDeltaSec { get; private set; }
        public double RawBoxDeltaSec { get; private set; }
        public bool BoxDeltaSuppressed { get; private set; }

        public double ServiceFuelAddedLitres { get; private set; }
        public double ServiceFuelTargetLitres { get; private set; }
        public double ServiceRefuelDurationSec { get; private set; }
        public double ServiceRefuelRateLps { get; private set; }
        public int ServiceTyreChangeCount { get; private set; }

        public double TimingPredictedTotalLossSec { get; private set; }
        public double TimingActualTotalLossSec { get; private set; }
        public double TimingLossDeltaSec { get; private set; }
        public string TimingLossSource { get; private set; } = "unavailable";

        public int ExitPredictedPositionInClass { get; private set; }
        public int ExitActualPositionInClass { get; private set; }
        public int ExitPositionDelta { get; private set; }
        public string ExitAccuracyText { get; private set; } = Unknown;

        public double GetAgeSec(DateTime utcNow)
        {
            if (!Valid || _finalizedUtc == DateTime.MinValue)
            {
                return 0.0;
            }

            double age = (utcNow - _finalizedUtc).TotalSeconds;
            return age > 0.0 && !double.IsNaN(age) && !double.IsInfinity(age) ? age : 0.0;
        }

        public void ResetAll()
        {
            _collecting = false;
            _finalLogPending = false;
            _finalizedUtc = DateTime.MinValue;
            _finalLogLine = string.Empty;
            StopIndex = 0;
            ClearDebriefValues();
        }

        public void StartPitEntry(double predictedTotalLossSec, int predictedPositionInClass, string entryDebriefToken, double entryLineTimeLossSec)
        {
            ClearDebriefValues();
            StopIndex++;
            _collecting = true;
            StateText = CollectingState;

            if (IsPositiveFinite(predictedTotalLossSec))
            {
                TimingPredictedTotalLossSec = predictedTotalLossSec;
                _hasPredictedTotalLoss = true;
            }

            if (predictedPositionInClass > 0)
            {
                ExitPredictedPositionInClass = predictedPositionInClass;
                _hasPredictedPosition = true;
            }

            LatchEntry(entryDebriefToken, entryLineTimeLossSec);
            RefreshProgressiveSummary();
        }

        public void RefreshEntryAssist(string entryDebriefToken, double entryLineTimeLossSec)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            LatchEntry(entryDebriefToken, entryLineTimeLossSec);
            RefreshProgressiveSummary();
        }

        public void ObservePitPhase(PitPhase phase)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            if (IsMissedBoxPhase(phase))
            {
                _missedBoxObserved = true;
                _missedBoxPhase = phase;
            }
        }

        public void LatchBoxEntry(double fuelTargetLitres, int tyreChangeCount, double serviceSec)
        {
            if (!_collecting || Valid || _boxSeen)
            {
                return;
            }

            _boxSeen = true;
            BoxQualityText = Unknown;
            BoxMissedReason = "unknown";

            LatchFuelTarget(fuelTargetLitres);

            if (tyreChangeCount >= 0)
            {
                ServiceTyreChangeCount = tyreChangeCount;
                _hasTyreCount = true;
            }

            if (IsFiniteNonNegative(serviceSec))
            {
                _hasServiceSec = true;
            }

            RefreshProgressiveSummary();
        }

        public void RefreshServiceEvidence(double fuelAddedLitres, double fuelTargetLitres, double refuelRateLps, bool fuelTargetClearedByInBoxCancel = false)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            if (fuelTargetClearedByInBoxCancel)
            {
                ClearFuelTarget();
            }
            else
            {
                LatchFuelTarget(fuelTargetLitres);
            }

            LatchFuelAdded(fuelAddedLitres);
            LatchRefuelRate(refuelRateLps);
            RefreshProgressiveSummary();
        }

        public void LatchBoxExit(double stationarySec, double fuelAddedLitres, double refuelRateLps, double refuelDurationSec, double boxDeltaSec, PitPhase finalPitPhase)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            ObservePitPhase(finalPitPhase);

            if (IsFiniteNonNegative(stationarySec))
            {
                BoxStationarySec = stationarySec;
                _hasServiceSec = true;
            }

            LatchFuelAdded(fuelAddedLitres);
            LatchRefuelRate(refuelRateLps);

            if (IsFiniteNonNegative(refuelDurationSec))
            {
                ServiceRefuelDurationSec = refuelDurationSec;
                _hasRefuelDuration = true;
            }

            LatchBoxDeltaFromActualMinusPredicted(boxDeltaSec);
            _serviceKnown = true;
            ResolveBoxOutcome();
            RefreshProgressiveSummary();
        }

        public void RefreshBoxDeltaFromActualMinusPredicted(double actualMinusPredictedSec)
        {
            if (!_collecting || Valid || !_boxSeen)
            {
                return;
            }

            LatchBoxDeltaFromActualMinusPredicted(actualMinusPredictedSec);
            RefreshProgressiveSummary();
        }

        public void RefreshBoxRepairInfluence(string repairInfluenceText)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            string normalized = NormalizeBoxRepairInfluence(repairInfluenceText);
            if (string.IsNullOrWhiteSpace(normalized) || string.Equals(normalized, _boxRepairInfluenceText, StringComparison.Ordinal))
            {
                return;
            }

            _boxRepairInfluenceText = normalized;
            if (_boxOutcomeResolved && !_missedBoxObserved)
            {
                BoxQualityText = normalized;
            }

            RefreshProgressiveSummary();
        }

        public void LatchPitLaneExit(int actualPositionInClass)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            if (string.Equals(StateText, CollectingState, StringComparison.Ordinal))
            {
                StateText = LaneExitState;
            }

            if (actualPositionInClass > 0)
            {
                ExitActualPositionInClass = actualPositionInClass;
                _hasActualPosition = true;
            }

            ResolveExitAccuracy();
            RefreshProgressiveSummary();
            if (!string.Equals(StateText, FinalState, StringComparison.Ordinal))
            {
                StateText = AwaitingOutLapState;
            }
        }

        public bool FinalizeDebrief(double actualTotalLossSec, string lossSource, DateTime utcNow)
        {
            if (!_collecting || Valid)
            {
                return false;
            }

            string normalizedSource = NormalizeLossSource(lossSource);
            TimingLossSource = normalizedSource;
            if (IsPositiveFinite(actualTotalLossSec))
            {
                TimingActualTotalLossSec = actualTotalLossSec;
                _hasActualTotalLoss = true;
            }
            else
            {
                TimingLossSource = "unavailable";
            }

            if (_hasActualTotalLoss && _hasPredictedTotalLoss)
            {
                TimingLossDeltaSec = TimingActualTotalLossSec - TimingPredictedTotalLossSec;
                _hasLossDelta = true;
            }

            ResolveBoxOutcome();
            ResolveExitAccuracy();
            _serviceKnown = true;
            SummaryText = BuildSummaryText();
            _summaryFinalized = true;
            Valid = true;
            _collecting = false;
            StateText = FinalState;
            _finalizedUtc = utcNow;
            _finalLogLine = BuildFinalLogLine();
            _finalLogPending = true;
            return true;
        }

        public bool TryConsumeFinalLogLine(out string logLine)
        {
            logLine = string.Empty;
            if (!_finalLogPending)
            {
                return false;
            }

            _finalLogPending = false;
            logLine = _finalLogLine ?? string.Empty;
            return !string.IsNullOrWhiteSpace(logLine);
        }

        private void ClearDebriefValues()
        {
            Valid = false;
            StateText = EmptyState;
            SummaryText = string.Empty;
            EntryQualityText = Unknown;
            EntryLineTimeLossSec = 0.0;
            EntryDecelQualityText = Unknown;
            EntryLimiterQualityText = Unknown;
            BoxQualityText = Unknown;
            BoxMissedReason = "unknown";
            BoxStationarySec = 0.0;
            BoxDeltaSec = 0.0;
            RawBoxDeltaSec = 0.0;
            BoxDeltaSuppressed = false;
            ServiceFuelAddedLitres = 0.0;
            ServiceFuelTargetLitres = 0.0;
            ServiceRefuelDurationSec = 0.0;
            ServiceRefuelRateLps = 0.0;
            ServiceTyreChangeCount = 0;
            TimingPredictedTotalLossSec = 0.0;
            TimingActualTotalLossSec = 0.0;
            TimingLossDeltaSec = 0.0;
            TimingLossSource = "unavailable";
            ExitPredictedPositionInClass = 0;
            ExitActualPositionInClass = 0;
            ExitPositionDelta = 0;
            ExitAccuracyText = Unknown;

            _hasEntryLoss = false;
            _hasPredictedTotalLoss = false;
            _hasActualTotalLoss = false;
            _hasLossDelta = false;
            _hasFuelTarget = false;
            _hasFuelAdded = false;
            _hasRefuelDuration = false;
            _hasRefuelRate = false;
            _hasTyreCount = false;
            _hasServiceSec = false;
            _hasBoxDelta = false;
            _hasRawBoxDelta = false;
            _boxDeltaSuppressed = false;
            _serviceKnown = false;
            _boxOutcomeResolved = false;
            _summaryFinalized = false;
            _hasPredictedPosition = false;
            _hasActualPosition = false;
            _hasPositionDelta = false;
            _boxSeen = false;
            _missedBoxObserved = false;
            _missedBoxPhase = PitPhase.None;
            _boxRepairInfluenceText = string.Empty;
            _finalLogPending = false;
            _finalLogLine = string.Empty;
            _finalizedUtc = DateTime.MinValue;
        }

        private void LatchEntry(string entryDebriefToken, double entryLineTimeLossSec)
        {
            string token = (entryDebriefToken ?? string.Empty).Trim().ToLowerInvariant();
            if (token == "safe")
            {
                EntryLimiterQualityText = "SAFE";
            }
            else if (token == "normal")
            {
                EntryLimiterQualityText = "NORMAL";
            }
            else if (token == "bad")
            {
                EntryLimiterQualityText = "POOR";
            }
            else
            {
                EntryLimiterQualityText = Unknown;
            }

            EntryDecelQualityText = Unknown;
            if (IsFiniteNonNegative(entryLineTimeLossSec))
            {
                EntryLineTimeLossSec = entryLineTimeLossSec;
                _hasEntryLoss = true;
                EntryQualityText = ResolveEntryPerformanceQuality(entryLineTimeLossSec);
            }
            else
            {
                EntryQualityText = ResolveEntryFallbackQuality(token);
            }
        }


        private static string ResolveEntryPerformanceQuality(double entryLineTimeLossSec)
        {
            if (entryLineTimeLossSec > 0.5) return "POOR";
            if (entryLineTimeLossSec > 0.1) return "NORMAL";
            return "GOOD";
        }

        private static string ResolveEntryFallbackQuality(string token)
        {
            if (token == "safe") return "GOOD";
            if (token == "normal") return "NORMAL";
            if (token == "bad") return "POOR";
            return Unknown;
        }

        private void LatchFuelTarget(double fuelTargetLitres)
        {
            if (!IsPositiveFinite(fuelTargetLitres))
            {
                return;
            }

            // Preserve the largest intended/requested add observed from existing Fuel.Pit seams so
            // a later box-exit MFD/gauge reset to 0 cannot erase completed-stop target evidence.
            // Explicit in-box refuel deselect uses ClearFuelTarget() before this positive latch.
            if (!_hasFuelTarget || fuelTargetLitres > ServiceFuelTargetLitres)
            {
                ServiceFuelTargetLitres = fuelTargetLitres;
            }

            _hasFuelTarget = true;
        }

        private void ClearFuelTarget()
        {
            ServiceFuelTargetLitres = 0.0;
            _hasFuelTarget = true;
        }

        private void LatchFuelAdded(double fuelAddedLitres)
        {
            // Preserve the largest existing Fuel.Pit gauge value observed during service so
            // a later box-exit reset to 0 cannot erase the completed-stop debrief evidence.
            if (!IsFiniteNonNegative(fuelAddedLitres))
            {
                return;
            }

            if (!_hasFuelAdded || fuelAddedLitres > ServiceFuelAddedLitres)
            {
                ServiceFuelAddedLitres = fuelAddedLitres;
            }

            _hasFuelAdded = true;
        }

        public void LatchRefuelDuration(double refuelDurationSec)
        {
            if (!_collecting || Valid || !IsFiniteNonNegative(refuelDurationSec))
            {
                return;
            }

            ServiceRefuelDurationSec = refuelDurationSec;
            _hasRefuelDuration = true;
            RefreshProgressiveSummary();
        }

        private void LatchBoxDeltaFromActualMinusPredicted(double actualMinusPredictedSec)
        {
            LatchBoxDeltaCandidate(actualMinusPredictedSec);
        }

        private void LatchBoxDeltaCandidate(double actualMinusPredictedSec)
        {
            if (double.IsNaN(actualMinusPredictedSec) || double.IsInfinity(actualMinusPredictedSec))
            {
                return;
            }

            RawBoxDeltaSec = actualMinusPredictedSec;
            _hasRawBoxDelta = true;

            BoxDeltaSec = actualMinusPredictedSec;
            _hasBoxDelta = true;
            BoxDeltaSuppressed = false;
            _boxDeltaSuppressed = false;
        }

        private void LatchRefuelRate(double refuelRateLps)
        {
            if (IsPositiveFinite(refuelRateLps))
            {
                ServiceRefuelRateLps = refuelRateLps;
                _hasRefuelRate = true;
            }
        }

        private void ResolveBoxOutcome()
        {
            if (_missedBoxObserved)
            {
                if (_missedBoxPhase == PitPhase.MissedBoxLong)
                {
                    BoxQualityText = "OVERSHOT";
                    BoxMissedReason = "long";
                }
                else if (_missedBoxPhase == PitPhase.MissedBoxShort)
                {
                    BoxQualityText = "MISSED";
                    BoxMissedReason = "short";
                }
                else if (_missedBoxPhase == PitPhase.MissedBoxLeft)
                {
                    BoxQualityText = "MISSED";
                    BoxMissedReason = "left";
                }
                else if (_missedBoxPhase == PitPhase.MissedBoxRight)
                {
                    BoxQualityText = "MISSED";
                    BoxMissedReason = "right";
                }
                else
                {
                    BoxQualityText = Unknown;
                    BoxMissedReason = "unknown";
                }

                _boxOutcomeResolved = true;
                return;
            }

            if (_boxSeen)
            {
                BoxQualityText = !string.IsNullOrWhiteSpace(_boxRepairInfluenceText) ? _boxRepairInfluenceText : "GOOD";
                BoxMissedReason = "none";
                _boxOutcomeResolved = true;
            }
            else
            {
                BoxQualityText = Unknown;
                BoxMissedReason = "unknown";
                _boxOutcomeResolved = true;
            }
        }

        private void RefreshProgressiveSummary()
        {
            if (_collecting && !_summaryFinalized)
            {
                SummaryText = BuildSummaryText();
            }
        }

        private string FormatEntrySection()
        {
            string quality;
            if (string.Equals(EntryQualityText, "POOR", StringComparison.Ordinal)) quality = "POOR";
            else if (string.Equals(EntryQualityText, "NORMAL", StringComparison.Ordinal)) quality = "NORMAL";
            else if (string.Equals(EntryQualityText, "GOOD", StringComparison.Ordinal)) quality = "GOOD";
            else quality = Unknown;

            string delta = _hasEntryLoss
                ? EntryLineTimeLossSec.ToString("+0.0;-0.0;+0.0", CultureInfo.InvariantCulture) + "s"
                : "PENDING";
            return "ENTRY " + quality + " (Δ " + delta + ")";
        }

        private string FormatBoxSection()
        {
            if (!_boxOutcomeResolved)
            {
                return "BOX PENDING";
            }

            string quality = string.IsNullOrWhiteSpace(BoxQualityText) ? Unknown : BoxQualityText;
            string delta = _hasBoxDelta
                ? BoxDeltaSec.ToString("+0.0;-0.0;+0.0", CultureInfo.InvariantCulture) + "s"
                : "PENDING";
            return "BOX " + quality + " (Δ " + delta + ")";
        }

        private string FormatServiceSection()
        {
            double fuel = _hasFuelAdded ? Math.Max(0.0, ServiceFuelAddedLitres) : 0.0;
            if (!_serviceKnown && fuel <= 0.05)
            {
                return "SVC PENDING";
            }

            int tyres = _hasTyreCount ? ServiceTyreChangeCount : 0;
            if (tyres < 0) tyres = 0;
            if (tyres > 4) tyres = 4;

            bool hasFuel = fuel > 0.05;
            bool hasTyres = tyres > 0;
            if (hasFuel && hasTyres)
            {
                return "SVC " + fuel.ToString("0.0", CultureInfo.InvariantCulture) + "L & " + tyres.ToString(CultureInfo.InvariantCulture) + "Ts";
            }

            if (hasFuel)
            {
                return "SVC " + fuel.ToString("0.0", CultureInfo.InvariantCulture) + "L";
            }

            if (hasTyres)
            {
                return "SVC " + tyres.ToString(CultureInfo.InvariantCulture) + "Ts";
            }

            return "NO SVC";
        }

        private string FormatStrategySection()
        {
            string delta = _hasLossDelta
                ? TimingLossDeltaSec.ToString("+0.0;-0.0;+0.0", CultureInfo.InvariantCulture) + "s"
                : "PENDING";
            return "STRAT Δ " + delta;
        }

        private void ResolveExitAccuracy()
        {
            if (!_hasPredictedPosition || !_hasActualPosition)
            {
                ExitPositionDelta = 0;
                _hasPositionDelta = false;
                ExitAccuracyText = Unknown;
                return;
            }

            ExitPositionDelta = ExitActualPositionInClass - ExitPredictedPositionInClass;
            _hasPositionDelta = true;
            int absDelta = Math.Abs(ExitPositionDelta);
            if (absDelta == 0)
            {
                ExitAccuracyText = "EXACT";
            }
            else if (absDelta == 1)
            {
                ExitAccuracyText = "CLOSE";
            }
            else
            {
                ExitAccuracyText = "MISS";
            }
        }

        private string BuildSummaryText()
        {
            return FormatEntrySection() + " | "
                + FormatBoxSection() + " | "
                + FormatServiceSection() + " | "
                + FormatStrategySection();
        }

        private string BuildFinalLogLine()
        {
            var sb = new StringBuilder(256);
            sb.Append("[LalaPlugin:PitDebrief] final");
            sb.Append(" stop=").Append(StopIndex.ToString(CultureInfo.InvariantCulture));
            sb.Append(" entry=").Append(EntryQualityText);
            sb.Append(" entryLossSec=").Append(FormatNumberOrNa(EntryLineTimeLossSec, _hasEntryLoss, "0.00"));
            sb.Append(" decelQuality=").Append(EntryDecelQualityText);
            sb.Append(" limiter=").Append(EntryLimiterQualityText);
            sb.Append(" box=").Append(BoxQualityText);
            sb.Append(" missed=").Append(BoxMissedReason);
            sb.Append(" boxDeltaSec=").Append(FormatNumberOrNa(BoxDeltaSec, _hasBoxDelta, "0.0"));
            sb.Append(" rawBoxDeltaSec=").Append(FormatNumberOrNa(RawBoxDeltaSec, _hasRawBoxDelta, "0.0"));
            sb.Append(" boxDeltaSuppressed=").Append(_boxDeltaSuppressed ? "true" : "false");
            sb.Append(" fuelTargetL=").Append(FormatNumberOrNa(ServiceFuelTargetLitres, _hasFuelTarget, "0.0"));
            sb.Append(" fuelAddedL=").Append(FormatNumberOrNa(ServiceFuelAddedLitres, _hasFuelAdded, "0.0"));
            sb.Append(" refuelSec=").Append(FormatNumberOrNa(ServiceRefuelDurationSec, _hasRefuelDuration, "0.0"));
            sb.Append(" refuelRateLps=").Append(FormatNumberOrNa(ServiceRefuelRateLps, _hasRefuelRate, "0.00"));
            sb.Append(" tyres=").Append(_hasTyreCount ? ServiceTyreChangeCount.ToString(CultureInfo.InvariantCulture) : "na");
            sb.Append(" serviceSec=").Append(FormatNumberOrNa(BoxStationarySec, _hasServiceSec, "0.0"));
            sb.Append(" predTotalLossSec=").Append(FormatNumberOrNa(TimingPredictedTotalLossSec, _hasPredictedTotalLoss, "0.0"));
            sb.Append(" actualLossSec=").Append(FormatNumberOrNa(TimingActualTotalLossSec, _hasActualTotalLoss, "0.0"));
            sb.Append(" lossDeltaSec=").Append(FormatNumberOrNa(TimingLossDeltaSec, _hasLossDelta, "0.0"));
            sb.Append(" lossSource=").Append(TimingLossSource);
            sb.Append(" exitPredPos=").Append(_hasPredictedPosition ? ExitPredictedPositionInClass.ToString(CultureInfo.InvariantCulture) : "na");
            sb.Append(" exitActualPos=").Append(_hasActualPosition ? ExitActualPositionInClass.ToString(CultureInfo.InvariantCulture) : "na");
            sb.Append(" exitPosDelta=").Append(_hasPositionDelta ? ExitPositionDelta.ToString(CultureInfo.InvariantCulture) : "na");
            sb.Append(" exitAccuracy=").Append(ExitAccuracyText);
            sb.Append(" summary=\"").Append((SummaryText ?? string.Empty).Replace("\"", "'")).Append("\"");
            return sb.ToString();
        }

        private static string FormatNumberOrNa(double value, bool hasValue, string format)
        {
            if (!hasValue || double.IsNaN(value) || double.IsInfinity(value))
            {
                return "na";
            }

            return value.ToString(format, CultureInfo.InvariantCulture);
        }

        private static bool IsMissedBoxPhase(PitPhase phase)
        {
            return phase == PitPhase.MissedBoxLong
                || phase == PitPhase.MissedBoxShort
                || phase == PitPhase.MissedBoxLeft
                || phase == PitPhase.MissedBoxRight;
        }

        private static string NormalizeBoxRepairInfluence(string repairInfluenceText)
        {
            string normalized = (repairInfluenceText ?? string.Empty).Trim().ToUpperInvariant();
            if (normalized == "MANDATORY" || normalized == "MAND" || normalized == "MAND REPAIR") return "MAND REPAIR";
            if (normalized == "OPTIONAL" || normalized == "OPT" || normalized == "OPT REPAIR") return "OPT REPAIR";
            if (normalized == "REPAIR" || normalized == "REPAIRS" || normalized == "GENERIC" || normalized == "UNKNOWN" || normalized == "BOX REPAIRS") return "REPAIRS";
            return string.Empty;
        }

        private static string NormalizeLossSource(string source)
        {
            string normalized = (source ?? string.Empty).Trim().ToLowerInvariant();
            if (normalized == "dtl" || normalized == "total") return "dtl";
            if (normalized == "direct") return "direct";
            return "unavailable";
        }

        private static bool IsPositiveFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }

        private static bool IsFiniteNonNegative(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0.0;
        }
    }
}
