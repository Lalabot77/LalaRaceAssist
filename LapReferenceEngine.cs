using System;

namespace LaunchPlugin
{
    public sealed class LapReferenceEngine
    {
        public const int SegmentCount = 6;
        public const int SegmentStateEmpty = 0;
        public const int SegmentStatePending = 1;
        public const int SegmentStateValid = 2;

        private readonly LapReferenceSnapshot _livePlayerCurrentLapSnapshot = new LapReferenceSnapshot();
        private readonly LapReferenceSnapshot _sessionBestSnapshot = new LapReferenceSnapshot();
        private readonly LapReferenceSnapshot _profileBestSnapshot = new LapReferenceSnapshot();

        private string _sessionToken = string.Empty;
        private string _sessionType = string.Empty;
        private string _carModel = string.Empty;
        private string _trackKey = string.Empty;
        private bool _isWet;
        private int _lastLivePlayerActiveSegment;
        private int _lastLivePlayerLapRef;

        public LapReferenceEngine()
        {
            Outputs = new LapReferenceOutputs();
        }

        public LapReferenceOutputs Outputs { get; private set; }

        public void Reset()
        {
            _sessionToken = string.Empty;
            _sessionType = string.Empty;
            _carModel = string.Empty;
            _trackKey = string.Empty;
            _isWet = false;

            _livePlayerCurrentLapSnapshot.Clear();
            _sessionBestSnapshot.Clear();
            _profileBestSnapshot.Clear();
            _lastLivePlayerActiveSegment = 0;
            _lastLivePlayerLapRef = 0;
            Outputs.Reset();
        }

        public void UpdateContext(
            string sessionToken,
            string sessionType,
            string carModel,
            string trackKey,
            bool isWet,
            int playerCarIdx,
            double playerLastLapTimeSec,
            int playerLapRef,
            int playerActiveSegment,
            double profileBestLapSec,
            int?[] profileBestSectorMs,
            bool hasLiveFixedSectorSnapshot,
            CarSAEngine.FixedSectorCacheSnapshot liveFixedSectorSnapshot)
        {
            string nextSessionToken = sessionToken ?? string.Empty;
            string nextSessionType = sessionType ?? string.Empty;
            string nextCarModel = carModel ?? string.Empty;
            string nextTrackKey = trackKey ?? string.Empty;

            bool contextChanged =
                !string.Equals(_sessionToken, nextSessionToken, StringComparison.Ordinal)
                || !string.Equals(_sessionType, nextSessionType, StringComparison.Ordinal)
                || !string.Equals(_carModel, nextCarModel, StringComparison.Ordinal)
                || !string.Equals(_trackKey, nextTrackKey, StringComparison.Ordinal)
                || _isWet != isWet;

            if (contextChanged)
            {
                _sessionToken = nextSessionToken;
                _sessionType = nextSessionType;
                _carModel = nextCarModel;
                _trackKey = nextTrackKey;
                _isWet = isWet;

                _livePlayerCurrentLapSnapshot.Clear();
                _sessionBestSnapshot.Clear();
                _lastLivePlayerActiveSegment = 0;
                _lastLivePlayerLapRef = 0;
            }

            MaterializeProfileBest(profileBestLapSec, profileBestSectorMs, isWet);

            Outputs.Valid = playerCarIdx >= 0 && !string.IsNullOrWhiteSpace(nextTrackKey) && !string.IsNullOrWhiteSpace(nextCarModel);
            Outputs.Mode = isWet ? "Wet" : "Dry";
            Outputs.PlayerCarIdx = playerCarIdx;
            Outputs.ActiveSegment = SanitizeSegment(playerActiveSegment);
            BuildLivePlayerCurrentLapSnapshot(Outputs.ActiveSegment, playerLapRef, hasLiveFixedSectorSnapshot, liveFixedSectorSnapshot);
            BuildLivePlayerOutput(Outputs.Player, Outputs.ActiveSegment, Outputs.Valid, playerLastLapTimeSec, hasLiveFixedSectorSnapshot, liveFixedSectorSnapshot);

            Outputs.SessionBest.AssignFromSnapshot(_sessionBestSnapshot);
            Outputs.ProfileBest.AssignFromSnapshot(_profileBestSnapshot);

            BuildComparison(Outputs.CompareSessionBest, _livePlayerCurrentLapSnapshot, _sessionBestSnapshot);
            BuildComparison(Outputs.CompareProfileBest, _livePlayerCurrentLapSnapshot, _profileBestSnapshot);

            BuildCumulativeDelta(_livePlayerCurrentLapSnapshot, _sessionBestSnapshot, out var deltaToSessionBestSec, out var deltaToSessionBestValid);
            BuildCumulativeDelta(_livePlayerCurrentLapSnapshot, _profileBestSnapshot, out var deltaToProfileBestSec, out var deltaToProfileBestValid);
            Outputs.DeltaToSessionBestSec = deltaToSessionBestSec;
            Outputs.DeltaToSessionBestValid = deltaToSessionBestValid;
            Outputs.DeltaToProfileBestSec = deltaToProfileBestSec;
            Outputs.DeltaToProfileBestValid = deltaToProfileBestValid;
        }

        public bool CaptureValidatedLap(
            double lapTimeSec,
            int lapRef,
            int activeSegment,
            bool isWet,
            string carModel,
            string trackKey,
            string sessionToken,
            bool hasFixedSectorSnapshot,
            CarSAEngine.FixedSectorCacheSnapshot fixedSectorSnapshot)
        {
            if (!IsValidLapTime(lapTimeSec))
            {
                return false;
            }

            var snapshot = new LapReferenceSnapshot
            {
                LapTimeSec = lapTimeSec,
                HasLapTime = true,
                ActiveSegment = SanitizeSegment(activeSegment),
                LapRef = lapRef,
                IsWet = isWet,
                CarModel = carModel ?? string.Empty,
                TrackKey = trackKey ?? string.Empty,
                SessionToken = sessionToken ?? string.Empty
            };

            if (hasFixedSectorSnapshot)
            {
                for (int i = 0; i < SegmentCount; i++)
                {
                    var sector = fixedSectorSnapshot.GetSector(i);
                    if (sector.HasValue && IsValidLapTime(sector.DurationSec))
                    {
                        snapshot.SetSector(i, true, sector.DurationSec);
                    }
                    else
                    {
                        snapshot.SetSector(i, false, 0.0);
                    }
                }
            }

            bool sessionBestImproved = !_sessionBestSnapshot.HasLapTime || lapTimeSec < _sessionBestSnapshot.LapTimeSec;
            if (sessionBestImproved)
            {
                _sessionBestSnapshot.CopyFrom(snapshot);
            }

            return sessionBestImproved;
        }

        private void MaterializeProfileBest(double profileBestLapSec, int?[] profileBestSectorMs, bool isWet)
        {
            _profileBestSnapshot.Clear();
            if (!IsValidLapTime(profileBestLapSec))
            {
                return;
            }

            _profileBestSnapshot.HasLapTime = true;
            _profileBestSnapshot.LapTimeSec = profileBestLapSec;
            _profileBestSnapshot.IsWet = isWet;
            _profileBestSnapshot.CarModel = _carModel;
            _profileBestSnapshot.TrackKey = _trackKey;
            _profileBestSnapshot.SessionToken = _sessionToken;
            _profileBestSnapshot.ActiveSegment = 0;
            _profileBestSnapshot.LapRef = 0;

            if (profileBestSectorMs == null)
            {
                return;
            }

            int limit = Math.Min(SegmentCount, profileBestSectorMs.Length);
            for (int i = 0; i < limit; i++)
            {
                int? sectorMs = profileBestSectorMs[i];
                if (sectorMs.HasValue && sectorMs.Value > 0)
                {
                    _profileBestSnapshot.SetSector(i, true, sectorMs.Value / 1000.0);
                }
            }
        }

        private static void BuildComparison(LapReferenceComparisonOutput output, LapReferenceSnapshot player, LapReferenceSnapshot reference)
        {
            if (output == null)
            {
                return;
            }

            for (int i = 0; i < SegmentCount; i++)
            {
                bool playerValid = player.GetSectorHasValue(i);
                bool referenceValid = reference.GetSectorHasValue(i);
                if (playerValid && referenceValid)
                {
                    double delta = player.GetSectorSec(i) - reference.GetSectorSec(i);
                    output.SetSegment(i, SegmentStateValid, delta);
                }
                else if (playerValid || referenceValid)
                {
                    output.SetSegment(i, SegmentStatePending, 0.0);
                }
                else
                {
                    output.SetSegment(i, SegmentStateEmpty, 0.0);
                }
            }
        }

        private static void BuildCumulativeDelta(
            LapReferenceSnapshot player,
            LapReferenceSnapshot reference,
            out double deltaSec,
            out bool isValid)
        {
            deltaSec = 0.0;
            isValid = false;

            double playerSum = 0.0;
            double referenceSum = 0.0;
            for (int i = 0; i < SegmentCount; i++)
            {
                bool playerValid = player.GetSectorHasValue(i);
                bool referenceValid = reference.GetSectorHasValue(i);
                if (!playerValid || !referenceValid)
                {
                    continue;
                }

                playerSum += player.GetSectorSec(i);
                referenceSum += reference.GetSectorSec(i);
                isValid = true;
            }

            if (isValid)
            {
                deltaSec = playerSum - referenceSum;
            }
        }

        private void BuildLivePlayerCurrentLapSnapshot(
            int activeSegment,
            int playerLapRef,
            bool hasLiveFixedSectorSnapshot,
            CarSAEngine.FixedSectorCacheSnapshot liveFixedSectorSnapshot)
        {
            int previousActiveSegment = _lastLivePlayerActiveSegment;
            _lastLivePlayerActiveSegment = activeSegment;

            bool hasLapRef = playerLapRef > 0;
            bool lapRefAdvanced = hasLapRef && _lastLivePlayerLapRef > 0 && playerLapRef != _lastLivePlayerLapRef;
            _lastLivePlayerLapRef = hasLapRef ? playerLapRef : _lastLivePlayerLapRef;

            if (lapRefAdvanced || IsLapRollover(previousActiveSegment, activeSegment))
            {
                _livePlayerCurrentLapSnapshot.Clear();
            }

            if (!hasLiveFixedSectorSnapshot)
            {
                return;
            }

            int completedSectorCount = GetCompletedSectorCount(activeSegment);
            if (completedSectorCount <= 0)
            {
                return;
            }

            for (int i = 0; i < completedSectorCount; i++)
            {
                var sector = liveFixedSectorSnapshot.GetSector(i);
                if (sector.HasValue && IsValidLapTime(sector.DurationSec))
                {
                    _livePlayerCurrentLapSnapshot.SetSector(i, true, sector.DurationSec);
                }
            }
        }

        private static bool IsLapRollover(int previousActiveSegment, int currentActiveSegment)
        {
            bool isNormalWrap =
                currentActiveSegment > 0
                && previousActiveSegment > 0
                && currentActiveSegment < previousActiveSegment;

            if (isNormalWrap)
            {
                return true;
            }

            bool isBoundaryTransitionIntoZero =
                previousActiveSegment > 1
                && currentActiveSegment == 0;

            return isBoundaryTransitionIntoZero;
        }

        private static void BuildLivePlayerOutput(
            LapReferenceSideOutput output,
            int activeSegment,
            bool isContextValid,
            double playerLastLapTimeSec,
            bool hasLiveFixedSectorSnapshot,
            CarSAEngine.FixedSectorCacheSnapshot liveFixedSectorSnapshot)
        {
            if (output == null)
            {
                return;
            }

            output.Reset();
            output.Valid = isContextValid;
            output.ActiveSegment = activeSegment;
            output.LapTimeSec = IsValidLapTime(playerLastLapTimeSec) ? playerLastLapTimeSec : 0.0;

            for (int i = 0; i < SegmentCount; i++)
            {
                if (!hasLiveFixedSectorSnapshot)
                {
                    output.SetSector(i, SegmentStateEmpty, 0.0);
                    continue;
                }

                var sector = liveFixedSectorSnapshot.GetSector(i);
                bool hasSector = sector.HasValue && IsValidLapTime(sector.DurationSec);
                output.SetSector(i, hasSector ? SegmentStateValid : SegmentStateEmpty, hasSector ? sector.DurationSec : 0.0);
            }
        }

        private static int GetCompletedSectorCount(int activeSegment)
        {
            int sanitized = SanitizeSegment(activeSegment);
            if (sanitized <= 1)
            {
                return 0;
            }

            int completed = sanitized - 1;
            if (completed < 0)
            {
                return 0;
            }

            if (completed > SegmentCount)
            {
                return SegmentCount;
            }

            return completed;
        }

        private static int SanitizeSegment(int segment)
        {
            if (segment < 0)
            {
                return 0;
            }

            if (segment > SegmentCount)
            {
                return SegmentCount;
            }

            return segment;
        }

        private static bool IsValidLapTime(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0;
        }

        internal sealed class LapReferenceSnapshot
        {
            private readonly bool[] _hasSector = new bool[SegmentCount];
            private readonly double[] _sectorSec = new double[SegmentCount];

            public bool HasLapTime;
            public double LapTimeSec;
            public int ActiveSegment;
            public int LapRef;
            public bool IsWet;
            public string CarModel = string.Empty;
            public string TrackKey = string.Empty;
            public string SessionToken = string.Empty;

            public void Clear()
            {
                HasLapTime = false;
                LapTimeSec = 0.0;
                ActiveSegment = 0;
                LapRef = 0;
                IsWet = false;
                CarModel = string.Empty;
                TrackKey = string.Empty;
                SessionToken = string.Empty;
                for (int i = 0; i < SegmentCount; i++)
                {
                    _hasSector[i] = false;
                    _sectorSec[i] = 0.0;
                }
            }

            public void CopyFrom(LapReferenceSnapshot source)
            {
                if (source == null)
                {
                    Clear();
                    return;
                }

                HasLapTime = source.HasLapTime;
                LapTimeSec = source.LapTimeSec;
                ActiveSegment = source.ActiveSegment;
                LapRef = source.LapRef;
                IsWet = source.IsWet;
                CarModel = source.CarModel ?? string.Empty;
                TrackKey = source.TrackKey ?? string.Empty;
                SessionToken = source.SessionToken ?? string.Empty;
                for (int i = 0; i < SegmentCount; i++)
                {
                    _hasSector[i] = source._hasSector[i];
                    _sectorSec[i] = source._sectorSec[i];
                }
            }

            public void SetSector(int index, bool hasValue, double valueSec)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return;
                }

                _hasSector[index] = hasValue;
                _sectorSec[index] = hasValue ? valueSec : 0.0;
            }

            public bool GetSectorHasValue(int index)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return false;
                }

                return _hasSector[index];
            }

            public double GetSectorSec(int index)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return 0.0;
                }

                return _hasSector[index] ? _sectorSec[index] : 0.0;
            }
        }

        public sealed class LapReferenceOutputs
        {
            public LapReferenceOutputs()
            {
                Player = new LapReferenceSideOutput();
                SessionBest = new LapReferenceSideOutput();
                ProfileBest = new LapReferenceSideOutput();
                CompareSessionBest = new LapReferenceComparisonOutput();
                CompareProfileBest = new LapReferenceComparisonOutput();
            }

            public bool Valid { get; set; }
            public string Mode { get; set; } = string.Empty;
            public int PlayerCarIdx { get; set; } = -1;
            public int ActiveSegment { get; set; }
            public double DeltaToSessionBestSec { get; set; }
            public bool DeltaToSessionBestValid { get; set; }
            public double DeltaToProfileBestSec { get; set; }
            public bool DeltaToProfileBestValid { get; set; }

            public LapReferenceSideOutput Player { get; private set; }
            public LapReferenceSideOutput SessionBest { get; private set; }
            public LapReferenceSideOutput ProfileBest { get; private set; }
            public LapReferenceComparisonOutput CompareSessionBest { get; private set; }
            public LapReferenceComparisonOutput CompareProfileBest { get; private set; }

            public void Reset()
            {
                Valid = false;
                Mode = string.Empty;
                PlayerCarIdx = -1;
                ActiveSegment = 0;
                DeltaToSessionBestSec = 0.0;
                DeltaToSessionBestValid = false;
                DeltaToProfileBestSec = 0.0;
                DeltaToProfileBestValid = false;
                Player.Reset();
                SessionBest.Reset();
                ProfileBest.Reset();
                CompareSessionBest.Reset();
                CompareProfileBest.Reset();
            }
        }

        public sealed class LapReferenceSideOutput
        {
            private readonly int[] _sectorState = new int[SegmentCount];
            private readonly double[] _sectorSec = new double[SegmentCount];

            public bool Valid { get; set; }
            public double LapTimeSec { get; set; }
            public int ActiveSegment { get; set; }

            public void Reset()
            {
                Valid = false;
                LapTimeSec = 0.0;
                ActiveSegment = 0;
                for (int i = 0; i < SegmentCount; i++)
                {
                    _sectorState[i] = SegmentStateEmpty;
                    _sectorSec[i] = 0.0;
                }
            }

            public int GetSectorState(int index)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return SegmentStateEmpty;
                }

                return _sectorState[index];
            }

            public double GetSectorSec(int index)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return 0.0;
                }

                return _sectorSec[index];
            }

            internal void AssignFromSnapshot(LapReferenceSnapshot snapshot)
            {
                if (snapshot == null || !snapshot.HasLapTime)
                {
                    Reset();
                    return;
                }

                Valid = true;
                LapTimeSec = snapshot.LapTimeSec;
                ActiveSegment = snapshot.ActiveSegment;
                for (int i = 0; i < SegmentCount; i++)
                {
                    bool hasSector = snapshot.GetSectorHasValue(i);
                    _sectorState[i] = hasSector ? SegmentStateValid : SegmentStateEmpty;
                    _sectorSec[i] = hasSector ? snapshot.GetSectorSec(i) : 0.0;
                }
            }

            internal void SetSector(int index, int state, double sectorSec)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return;
                }

                _sectorState[index] = state;
                _sectorSec[index] = state == SegmentStateValid ? sectorSec : 0.0;
            }
        }

        public sealed class LapReferenceComparisonOutput
        {
            private readonly int[] _sectorState = new int[SegmentCount];
            private readonly double[] _sectorDeltaSec = new double[SegmentCount];

            public void Reset()
            {
                for (int i = 0; i < SegmentCount; i++)
                {
                    _sectorState[i] = SegmentStateEmpty;
                    _sectorDeltaSec[i] = 0.0;
                }
            }

            public int GetSectorState(int index)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return SegmentStateEmpty;
                }

                return _sectorState[index];
            }

            public double GetSectorDeltaSec(int index)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return 0.0;
                }

                return _sectorDeltaSec[index];
            }

            public void SetSegment(int index, int state, double deltaSec)
            {
                if (index < 0 || index >= SegmentCount)
                {
                    return;
                }

                _sectorState[index] = state;
                _sectorDeltaSec[index] = state == SegmentStateValid ? deltaSec : 0.0;
            }
        }
    }
}
