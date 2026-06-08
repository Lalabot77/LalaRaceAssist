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
        private bool _hasPredictedPosition;
        private bool _hasActualPosition;
        private bool _hasPositionDelta;
        private bool _boxSeen;
        private bool _missedBoxObserved;
        private PitPhase _missedBoxPhase = PitPhase.None;

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
        }

        public void RefreshEntryAssist(string entryDebriefToken, double entryLineTimeLossSec)
        {
            if (!_collecting || Valid)
            {
                return;
            }

            LatchEntry(entryDebriefToken, entryLineTimeLossSec);
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

            if (IsFiniteNonNegative(fuelTargetLitres))
            {
                ServiceFuelTargetLitres = fuelTargetLitres;
                _hasFuelTarget = true;
            }

            if (tyreChangeCount >= 0)
            {
                ServiceTyreChangeCount = tyreChangeCount;
                _hasTyreCount = true;
            }

            if (IsFiniteNonNegative(serviceSec))
            {
                _hasServiceSec = true;
            }
        }

        public void LatchBoxExit(double stationarySec, double fuelAddedLitres, double refuelRateLps, double refuelDurationSec, PitPhase finalPitPhase)
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

            if (IsFiniteNonNegative(fuelAddedLitres))
            {
                ServiceFuelAddedLitres = fuelAddedLitres;
                _hasFuelAdded = true;
            }

            if (IsPositiveFinite(refuelRateLps))
            {
                ServiceRefuelRateLps = refuelRateLps;
                _hasRefuelRate = true;
            }

            if (IsFiniteNonNegative(refuelDurationSec))
            {
                ServiceRefuelDurationSec = refuelDurationSec;
                _hasRefuelDuration = true;
            }

            ResolveBoxOutcome();
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
            SummaryText = BuildSummaryText();
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
            _hasPredictedPosition = false;
            _hasActualPosition = false;
            _hasPositionDelta = false;
            _boxSeen = false;
            _missedBoxObserved = false;
            _missedBoxPhase = PitPhase.None;
            _finalLogPending = false;
            _finalLogLine = string.Empty;
            _finalizedUtc = DateTime.MinValue;
        }

        private void LatchEntry(string entryDebriefToken, double entryLineTimeLossSec)
        {
            string token = (entryDebriefToken ?? string.Empty).Trim().ToLowerInvariant();
            if (token == "safe")
            {
                EntryQualityText = "GOOD";
                EntryLimiterQualityText = "SAFE";
            }
            else if (token == "normal")
            {
                EntryQualityText = "NORMAL";
                EntryLimiterQualityText = "NORMAL";
            }
            else if (token == "bad")
            {
                EntryQualityText = "POOR";
                EntryLimiterQualityText = "POOR";
            }
            else
            {
                EntryQualityText = Unknown;
                EntryLimiterQualityText = Unknown;
            }

            EntryDecelQualityText = Unknown;
            if (IsFiniteNonNegative(entryLineTimeLossSec))
            {
                EntryLineTimeLossSec = entryLineTimeLossSec;
                _hasEntryLoss = true;
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

                return;
            }

            if (_boxSeen)
            {
                BoxQualityText = "GOOD";
                BoxMissedReason = "none";
            }
            else
            {
                BoxQualityText = Unknown;
                BoxMissedReason = "unknown";
            }
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
            string lossText = _hasLossDelta
                ? TimingLossDeltaSec.ToString("+0.0;-0.0;0.0", CultureInfo.InvariantCulture) + "s"
                : "NA";
            string exitText;
            if (string.Equals(ExitAccuracyText, "EXACT", StringComparison.Ordinal)) exitText = "OK";
            else if (string.Equals(ExitAccuracyText, "CLOSE", StringComparison.Ordinal)) exitText = "CLOSE";
            else if (string.Equals(ExitAccuracyText, "MISS", StringComparison.Ordinal)) exitText = "MISS";
            else exitText = Unknown;

            if (_hasActualPosition && ExitActualPositionInClass > 0)
            {
                exitText += " P" + ExitActualPositionInClass.ToString(CultureInfo.InvariantCulture);
            }

            return "ENTRY " + EntryQualityText + " | BOX " + BoxQualityText + " | LOSS " + lossText + " | EXIT " + exitText;
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
