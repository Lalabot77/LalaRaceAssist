using System;

namespace LaunchPlugin
{
    public sealed class H2HEngine
    {
        public const int SegmentCount = 6;
        public const int SegmentStateEmpty = 0;
        public const int SegmentStatePending = 1;
        public const int SegmentStateValid = 2;

        private const double LiveDeltaClampSec = 30.0;
        private const double DefaultGapLapTimeSec = 120.0;
        private const double LapWrapThresholdPct = 0.20;
        private const double LapStartDetectWindowPct = 0.05;
        private const double LapTimeEqualityToleranceSec = 0.002;

        private const string LastLapColorNormal = "#FFFFFF";
        private const string LastLapColorPersonalBest = "#00FF00";
        private const string LastLapColorSessionBest = "#FF00FF";

        private readonly FamilyRuntime _raceRuntime = new FamilyRuntime();
        private readonly FamilyRuntime _trackRuntime = new FamilyRuntime();

        public delegate bool FixedSectorCacheReader(int carIdx, out CarSAEngine.FixedSectorCacheSnapshot snapshot);

        public H2HEngine()
        {
            Outputs = new H2HOutputs();
        }

        public H2HOutputs Outputs { get; private set; }

        public void Reset()
        {
            Outputs.Reset();
            _raceRuntime.Reset();
            _trackRuntime.Reset();
        }

        public void Update(
            double sessionTimeSec,
            int playerCarIdx,
            int playerPositionInClass,
            float[] carIdxLapDistPct,
            int[] carIdxLap,
            double playerBestLapSec,
            double playerLastLapSec,
            double classSessionBestLapSec,
            double[] bestLapTimeSecByIdx,
            double[] lastLapTimeSecByIdx,
            int[] classPositionByIdx,
            FixedSectorCacheReader fixedSectorCacheReader,
            TargetSelector raceAheadSelector,
            TargetSelector raceBehindSelector,
            TargetSelector trackAheadSelector,
            TargetSelector trackBehindSelector)
        {
            UpdateFamily(Outputs.Race, _raceRuntime, sessionTimeSec, playerCarIdx, carIdxLapDistPct, carIdxLap, playerBestLapSec, playerLastLapSec,
                playerPositionInClass, classSessionBestLapSec, bestLapTimeSecByIdx, lastLapTimeSecByIdx, classPositionByIdx, fixedSectorCacheReader, raceAheadSelector, raceBehindSelector);

            UpdateFamily(Outputs.Track, _trackRuntime, sessionTimeSec, playerCarIdx, carIdxLapDistPct, carIdxLap, playerBestLapSec, playerLastLapSec,
                playerPositionInClass, classSessionBestLapSec, bestLapTimeSecByIdx, lastLapTimeSecByIdx, classPositionByIdx, fixedSectorCacheReader, trackAheadSelector, trackBehindSelector);
        }

        private static void UpdateFamily(
            H2HFamilyOutput family,
            FamilyRuntime runtime,
            double sessionTimeSec,
            int playerCarIdx,
            float[] carIdxLapDistPct,
            int[] carIdxLap,
            double playerBestLapSec,
            double playerLastLapSec,
            int playerPositionInClass,
            double classSessionBestLapSec,
            double[] bestLapTimeSecByIdx,
            double[] lastLapTimeSecByIdx,
            int[] classPositionByIdx,
            FixedSectorCacheReader fixedSectorCacheReader,
            TargetSelector aheadSelector,
            TargetSelector behindSelector)
        {
            if (family == null || runtime == null)
            {
                return;
            }

            family.ClassSessionBestLapSec = SanitizeLapTime(classSessionBestLapSec);

            UpdatePlayer(family.Player, runtime.Player, sessionTimeSec, playerCarIdx, playerPositionInClass, carIdxLapDistPct, carIdxLap, playerBestLapSec, playerLastLapSec, classSessionBestLapSec);
            UpdateTarget(family.Ahead, runtime.Ahead, aheadSelector, sessionTimeSec, runtime.Player, family.Player,
                carIdxLapDistPct, carIdxLap, bestLapTimeSecByIdx, lastLapTimeSecByIdx, classPositionByIdx, classSessionBestLapSec, playerCarIdx, fixedSectorCacheReader);
            UpdateTarget(family.Behind, runtime.Behind, behindSelector, sessionTimeSec, runtime.Player, family.Player,
                carIdxLapDistPct, carIdxLap, bestLapTimeSecByIdx, lastLapTimeSecByIdx, classPositionByIdx, classSessionBestLapSec, playerCarIdx, fixedSectorCacheReader);
        }

        private static void UpdatePlayer(
            H2HParticipantOutput output,
            ParticipantRuntime runtime,
            double sessionTimeSec,
            int playerCarIdx,
            int playerPositionInClass,
            float[] carIdxLapDistPct,
            int[] carIdxLap,
            double playerBestLapSec,
            double playerLastLapSec,
            double classSessionBestLapSec)
        {
            if (output == null || runtime == null)
            {
                return;
            }

            bool hasContext = UpdateRuntime(runtime, playerCarIdx, sessionTimeSec, carIdxLapDistPct, carIdxLap);
            output.LastLapSec = SanitizeLapTime(playerLastLapSec);
            output.BestLapSec = SanitizeLapTime(playerBestLapSec);
            runtime.BestLapReferenceSec = output.BestLapSec;
            runtime.LastLapReferenceSec = output.LastLapSec;
            output.LastLapDeltaToBestSec = ComputeLastLapDeltaToBest(output.LastLapSec, output.BestLapSec);
            output.LiveDeltaToBestSec = hasContext ? ComputeLiveDeltaToBest(runtime, sessionTimeSec, output.BestLapSec) : 0.0;
            output.LastLapColor = ComputeLastLapColor(output.LastLapSec, output.BestLapSec, classSessionBestLapSec);
            output.PositionInClass = playerPositionInClass > 0 ? playerPositionInClass : 0;
            output.ActiveSegment = hasContext ? runtime.ActiveSegment : 0;
            output.LapRef = hasContext ? runtime.LapRef : 0;
        }

        private static void UpdateTarget(
            H2HParticipantOutput output,
            ParticipantRuntime runtime,
            TargetSelector selector,
            double sessionTimeSec,
            ParticipantRuntime playerRuntime,
            H2HParticipantOutput playerOutput,
            float[] carIdxLapDistPct,
            int[] carIdxLap,
            double[] bestLapTimeSecByIdx,
            double[] lastLapTimeSecByIdx,
            int[] classPositionByIdx,
            double classSessionBestLapSec,
            int playerCarIdx,
            FixedSectorCacheReader fixedSectorCacheReader)
        {
            if (output == null || runtime == null)
            {
                return;
            }

            bool hasSelector = !string.IsNullOrWhiteSpace(selector.IdentityKey) || selector.CarIdx >= 0;
            if (!hasSelector)
            {
                output.ResetTarget();
                runtime.Reset();
                return;
            }

            bool identityChanged = !string.Equals(output.IdentityKey ?? string.Empty, selector.IdentityKey ?? string.Empty, StringComparison.Ordinal);
            bool carIdxChanged = output.CarIdx != selector.CarIdx;
            if (identityChanged || carIdxChanged)
            {
                runtime.ResetContext(selector.CarIdx);
            }

            output.IdentityKey = selector.IdentityKey ?? string.Empty;
            output.Name = selector.Name ?? string.Empty;
            output.CarNumber = selector.CarNumber ?? string.Empty;
            output.ClassColor = selector.ClassColor ?? string.Empty;
            output.CarIdx = selector.CarIdx;

            int selectorPosClass = selector.PositionInClass;
            int resolvedPosClass = ReadClassPosition(classPositionByIdx, selector.CarIdx);
            output.PositionInClass = selectorPosClass > 0 ? selectorPosClass : resolvedPosClass;

            bool hasTargetContext = UpdateRuntime(runtime, selector.CarIdx, sessionTimeSec, carIdxLapDistPct, carIdxLap);
            bool hasPlayerContext = playerRuntime != null && playerRuntime.HasUsableContext;
            bool hasUsableTimingContext = hasTargetContext && hasPlayerContext;

            output.BestLapSec = ReadLapTime(bestLapTimeSecByIdx, selector.CarIdx);
            output.LastLapSec = ReadLapTime(lastLapTimeSecByIdx, selector.CarIdx);
            runtime.BestLapReferenceSec = output.BestLapSec;
            runtime.LastLapReferenceSec = output.LastLapSec;
            output.LastLapDeltaToBestSec = ComputeLastLapDeltaToBest(output.LastLapSec, output.BestLapSec);
            output.LiveDeltaToBestSec = hasTargetContext ? ComputeLiveDeltaToBest(runtime, sessionTimeSec, output.BestLapSec) : 0.0;
            output.LastLapColor = ComputeLastLapColor(output.LastLapSec, output.BestLapSec, classSessionBestLapSec);
            output.LastLapDeltaToPlayerSec = ComputeLastLapDeltaToPlayer(output.LastLapSec, playerOutput != null ? playerOutput.LastLapSec : 0.0);
            output.LiveGapSec = hasUsableTimingContext ? ComputeLiveGapSec(playerRuntime, runtime) : 0.0;
            output.ActiveSegment = hasTargetContext ? runtime.ActiveSegment : 0;
            output.LapRef = hasTargetContext ? runtime.LapRef : 0;
            output.Valid = hasUsableTimingContext;

            UpdateSegmentOutputs(output, playerCarIdx, selector.CarIdx, fixedSectorCacheReader);
        }

        private static void UpdateSegmentOutputs(H2HParticipantOutput output, int playerCarIdx, int targetCarIdx, FixedSectorCacheReader fixedSectorCacheReader)
        {
            if (output == null)
            {
                return;
            }

            for (int i = 0; i < SegmentCount; i++)
            {
                output.SetSegment(i, 0.0, SegmentStateEmpty);
            }

            if (fixedSectorCacheReader == null)
            {
                return;
            }

            CarSAEngine.FixedSectorCacheSnapshot playerSnapshot;
            CarSAEngine.FixedSectorCacheSnapshot targetSnapshot;
            bool hasPlayerSnapshot = fixedSectorCacheReader(playerCarIdx, out playerSnapshot);
            bool hasTargetSnapshot = fixedSectorCacheReader(targetCarIdx, out targetSnapshot);

            for (int i = 0; i < SegmentCount; i++)
            {
                if (!hasTargetSnapshot)
                {
                    continue;
                }

                CarSAEngine.FixedSectorValue targetSector = targetSnapshot.GetSector(i);
                if (!targetSector.HasValue || !IsValidSectorDuration(targetSector.DurationSec))
                {
                    continue;
                }

                if (!hasPlayerSnapshot)
                {
                    output.SetSegment(i, 0.0, SegmentStatePending);
                    continue;
                }

                CarSAEngine.FixedSectorValue playerSector = playerSnapshot.GetSector(i);
                if (!playerSector.HasValue || !IsValidSectorDuration(playerSector.DurationSec))
                {
                    output.SetSegment(i, 0.0, SegmentStatePending);
                    continue;
                }

                output.SetSegment(i, targetSector.DurationSec - playerSector.DurationSec, SegmentStateValid);
            }
        }

        private static bool UpdateRuntime(ParticipantRuntime runtime, int carIdx, double sessionTimeSec, float[] carIdxLapDistPct, int[] carIdxLap)
        {
            if (runtime == null)
            {
                return false;
            }

            if (carIdx < 0 || carIdxLapDistPct == null || carIdxLap == null || carIdx >= carIdxLapDistPct.Length || carIdx >= carIdxLap.Length)
            {
                runtime.Reset();
                return false;
            }

            double lapPct = carIdxLapDistPct[carIdx];
            int lapRef = carIdxLap[carIdx];
            if (!IsValidLapPct(lapPct))
            {
                runtime.ResetContext(carIdx);
                return false;
            }

            bool carChanged = runtime.CarIdx != carIdx;
            bool hadPriorSampleForCurrentCar = runtime.HasSample && !carChanged;
            bool lapRefChanged = runtime.HasSample && runtime.LapRef != lapRef;
            bool wrapped = runtime.HasSample && !double.IsNaN(runtime.LastLapPct) && (lapPct + LapWrapThresholdPct) < runtime.LastLapPct;
            bool newLap = carChanged || !runtime.HasSample || lapRefChanged || wrapped;

            if (carChanged)
            {
                runtime.ResetContext(carIdx);
            }
            else if (newLap)
            {
                runtime.ResetForNewLap();
                runtime.CarIdx = carIdx;
            }

            runtime.CarIdx = carIdx;
            runtime.HasSample = true;
            runtime.HasUsableContext = true;
            runtime.LapRef = lapRef;
            runtime.LapPct = lapPct;
            runtime.ActiveSegment = ComputeActiveSegment(lapPct);
            if (newLap || !IsFinite(runtime.LapStartTimeSec))
            {
                runtime.LapStartTimeSec = (!hadPriorSampleForCurrentCar && lapPct > LapStartDetectWindowPct)
                    ? double.NaN
                    : sessionTimeSec;
            }

            if (!newLap && runtime.LastActiveSegment > 0 && runtime.ActiveSegment != runtime.LastActiveSegment)
            {
                if (runtime.ActiveSegment < runtime.LastActiveSegment)
                {
                    runtime.ResetForNewLap();
                    runtime.CarIdx = carIdx;
                    runtime.HasSample = true;
                    runtime.HasUsableContext = true;
                    runtime.LapRef = lapRef;
                    runtime.LapPct = lapPct;
                    runtime.ActiveSegment = ComputeActiveSegment(lapPct);
                    runtime.LapStartTimeSec = sessionTimeSec;
                }
            }

            runtime.LastLapPct = lapPct;
            runtime.LastActiveSegment = runtime.ActiveSegment;
            return true;
        }

        private static double ComputeLiveDeltaToBest(ParticipantRuntime runtime, double sessionTimeSec, double bestLapSec)
        {
            if (runtime == null || !runtime.HasUsableContext || !IsValidLapTime(bestLapSec) || !IsFinite(runtime.LapStartTimeSec))
            {
                return 0.0;
            }

            double expected = bestLapSec * runtime.LapPct;
            double elapsed = sessionTimeSec - runtime.LapStartTimeSec;
            if (!IsFinite(elapsed) || elapsed < 0.0)
            {
                return 0.0;
            }

            double delta = elapsed - expected;
            if (delta > LiveDeltaClampSec)
            {
                delta = LiveDeltaClampSec;
            }
            else if (delta < -LiveDeltaClampSec)
            {
                delta = -LiveDeltaClampSec;
            }

            return delta;
        }

        private static double ComputeLiveGapSec(ParticipantRuntime playerRuntime, ParticipantRuntime targetRuntime)
        {
            if (playerRuntime == null || targetRuntime == null || !playerRuntime.HasUsableContext || !targetRuntime.HasUsableContext)
            {
                return 0.0;
            }

            double lapScaleSec = IsValidLapTime(playerRuntime.BestLapReferenceSec)
                ? playerRuntime.BestLapReferenceSec
                : (IsValidLapTime(playerRuntime.LastLapReferenceSec) ? playerRuntime.LastLapReferenceSec : DefaultGapLapTimeSec);

            if (!IsFinite(lapScaleSec) || lapScaleSec <= 0.0)
            {
                lapScaleSec = DefaultGapLapTimeSec;
            }

            double lapDelta = targetRuntime.LapRef - playerRuntime.LapRef;
            double pctDelta = targetRuntime.LapPct - playerRuntime.LapPct;
            double totalDelta = (lapDelta + pctDelta) * lapScaleSec;
            if (!IsFinite(totalDelta))
            {
                return 0.0;
            }

            return Math.Abs(totalDelta);
        }

        private static double ComputeLastLapDeltaToBest(double lastLapSec, double bestLapSec)
        {
            if (!IsValidLapTime(lastLapSec) || !IsValidLapTime(bestLapSec))
            {
                return 0.0;
            }

            return lastLapSec - bestLapSec;
        }

        private static double ComputeLastLapDeltaToPlayer(double targetLastLapSec, double playerLastLapSec)
        {
            if (!IsValidLapTime(targetLastLapSec) || !IsValidLapTime(playerLastLapSec))
            {
                return 0.0;
            }

            return targetLastLapSec - playerLastLapSec;
        }

        private static string ComputeLastLapColor(double lastLapSec, double bestLapSec, double classSessionBestLapSec)
        {
            bool lastLapMatchesBest = IsValidLapTime(lastLapSec)
                && IsValidLapTime(bestLapSec)
                && AreLapTimesEqual(lastLapSec, bestLapSec);
            if (!lastLapMatchesBest)
            {
                return LastLapColorNormal;
            }

            if (IsValidLapTime(classSessionBestLapSec) && AreLapTimesEqual(bestLapSec, classSessionBestLapSec))
            {
                return LastLapColorSessionBest;
            }

            return LastLapColorPersonalBest;
        }

        private static int ComputeActiveSegment(double lapPct)
        {
            if (!IsValidLapPct(lapPct))
            {
                return 0;
            }

            int segment = (int)Math.Floor(lapPct * SegmentCount) + 1;
            if (segment < 1)
            {
                return 1;
            }

            if (segment > SegmentCount)
            {
                return SegmentCount;
            }

            return segment;
        }

        private static bool IsValidLapPct(double lapPct)
        {
            return IsFinite(lapPct) && lapPct >= 0.0 && lapPct < 1.0;
        }

        private static bool IsValidLapTime(double lapTimeSec)
        {
            return IsFinite(lapTimeSec) && lapTimeSec > 0.0;
        }

        private static bool IsValidSectorDuration(double durationSec)
        {
            return IsFinite(durationSec) && durationSec > 0.0;
        }

        private static double SanitizeLapTime(double lapTimeSec)
        {
            return IsValidLapTime(lapTimeSec) ? lapTimeSec : 0.0;
        }

        private static double ReadLapTime(double[] values, int carIdx)
        {
            if (values == null || carIdx < 0 || carIdx >= values.Length)
            {
                return 0.0;
            }

            return SanitizeLapTime(values[carIdx]);
        }

        private static int ReadClassPosition(int[] values, int carIdx)
        {
            if (values == null || carIdx < 0 || carIdx >= values.Length)
            {
                return 0;
            }

            return values[carIdx] > 0 ? values[carIdx] : 0;
        }

        private static bool AreLapTimesEqual(double a, double b)
        {
            return IsValidLapTime(a) && IsValidLapTime(b) && Math.Abs(a - b) <= LapTimeEqualityToleranceSec;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }

        private sealed class FamilyRuntime
        {
            public readonly ParticipantRuntime Player = new ParticipantRuntime();
            public readonly ParticipantRuntime Ahead = new ParticipantRuntime();
            public readonly ParticipantRuntime Behind = new ParticipantRuntime();

            public void Reset()
            {
                Player.Reset();
                Ahead.Reset();
                Behind.Reset();
            }
        }

        private sealed class ParticipantRuntime
        {
            public int CarIdx = -1;
            public int LapRef;
            public double LapPct = double.NaN;
            public double LastLapPct = double.NaN;
            public int ActiveSegment;
            public int LastActiveSegment;
            public bool HasSample;
            public bool HasUsableContext;
            public double LapStartTimeSec = double.NaN;
            public double BestLapReferenceSec;
            public double LastLapReferenceSec;

            public ParticipantRuntime()
            {
                Reset();
            }

            public void Reset()
            {
                CarIdx = -1;
                LapRef = 0;
                LapPct = double.NaN;
                LastLapPct = double.NaN;
                ActiveSegment = 0;
                LastActiveSegment = 0;
                HasSample = false;
                HasUsableContext = false;
                LapStartTimeSec = double.NaN;
                BestLapReferenceSec = 0.0;
                LastLapReferenceSec = 0.0;
            }

            public void ResetForNewLap()
            {
                LapPct = double.NaN;
                LastLapPct = double.NaN;
                ActiveSegment = 0;
                LastActiveSegment = 0;
                HasUsableContext = false;
                LapStartTimeSec = double.NaN;
            }

            public void ResetContext(int carIdx)
            {
                CarIdx = carIdx;
                LapPct = double.NaN;
                LastLapPct = double.NaN;
                ActiveSegment = 0;
                LastActiveSegment = 0;
                HasUsableContext = false;
                HasSample = false;
                LapStartTimeSec = double.NaN;
            }
        }

        public sealed class H2HOutputs
        {
            public H2HOutputs()
            {
                Race = new H2HFamilyOutput();
                Track = new H2HFamilyOutput();
            }

            public H2HFamilyOutput Race { get; private set; }
            public H2HFamilyOutput Track { get; private set; }

            public void Reset()
            {
                Race.Reset();
                Track.Reset();
            }
        }

        public sealed class H2HFamilyOutput
        {
            public H2HFamilyOutput()
            {
                Player = new H2HParticipantOutput();
                Ahead = new H2HParticipantOutput();
                Behind = new H2HParticipantOutput();
            }

            public H2HParticipantOutput Player { get; private set; }
            public H2HParticipantOutput Ahead { get; private set; }
            public H2HParticipantOutput Behind { get; private set; }
            public double ClassSessionBestLapSec { get; set; }

            public void Reset()
            {
                ClassSessionBestLapSec = 0.0;
                Player.ResetPlayer();
                Ahead.ResetTarget();
                Behind.ResetTarget();
            }
        }

        public sealed class H2HParticipantOutput
        {
            private readonly double[] _segmentDeltaSec = new double[SegmentCount];
            private readonly int[] _segmentState = new int[SegmentCount];

            public bool Valid { get; set; }
            public int CarIdx { get; set; }
            public string IdentityKey { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string CarNumber { get; set; } = string.Empty;
            public string ClassColor { get; set; } = string.Empty;
            public int PositionInClass { get; set; }
            public double LastLapSec { get; set; }
            public double BestLapSec { get; set; }
            public double LastLapDeltaToBestSec { get; set; }
            public double LiveDeltaToBestSec { get; set; }
            public string LastLapColor { get; set; } = LastLapColorNormal;
            public double LastLapDeltaToPlayerSec { get; set; }
            public double LiveGapSec { get; set; }
            public int ActiveSegment { get; set; }
            public int LapRef { get; set; }

            public double GetSegmentDeltaSec(int index)
            {
                return index >= 0 && index < _segmentDeltaSec.Length ? _segmentDeltaSec[index] : 0.0;
            }

            public int GetSegmentState(int index)
            {
                return index >= 0 && index < _segmentState.Length ? _segmentState[index] : SegmentStateEmpty;
            }

            public void SetSegment(int index, double deltaSec, int state)
            {
                if (index < 0 || index >= _segmentDeltaSec.Length)
                {
                    return;
                }

                _segmentDeltaSec[index] = IsFinite(deltaSec) ? deltaSec : 0.0;
                _segmentState[index] = state;
            }

            public void ResetPlayer()
            {
                Valid = false;
                CarIdx = -1;
                IdentityKey = string.Empty;
                Name = string.Empty;
                CarNumber = string.Empty;
                ClassColor = string.Empty;
                PositionInClass = 0;
                LastLapSec = 0.0;
                BestLapSec = 0.0;
                LastLapDeltaToBestSec = 0.0;
                LiveDeltaToBestSec = 0.0;
                LastLapColor = LastLapColorNormal;
                LastLapDeltaToPlayerSec = 0.0;
                LiveGapSec = 0.0;
                ActiveSegment = 0;
                LapRef = 0;
                ResetSegments();
            }

            public void ResetTarget()
            {
                ResetPlayer();
            }

            private void ResetSegments()
            {
                for (int i = 0; i < SegmentCount; i++)
                {
                    _segmentDeltaSec[i] = 0.0;
                    _segmentState[i] = SegmentStateEmpty;
                }
            }
        }

        public struct TargetSelector
        {
            public int CarIdx;
            public string IdentityKey;
            public string Name;
            public string CarNumber;
            public string ClassColor;
            public int PositionInClass;
        }
    }
}
