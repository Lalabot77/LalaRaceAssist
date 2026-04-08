using GameReaderCommon;
using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LaunchPlugin
{
    public class OpponentsEngine
    {
        private readonly EntityCache _entityCache = new EntityCache();
        private readonly NativeRaceModel _raceModel = new NativeRaceModel();
        private readonly PitExitPredictor _pitExitPredictor;
        private TryGetCheckpointGapSec _tryGetCheckpointGapSec;

        private bool _gateActive;
        private bool _gateOpenedLogged;
        private bool _pitExitWasRaceActive;
        private string _playerIdentityKey = string.Empty;
        private DateTime _lastNativeInvalidLogUtc = DateTime.MinValue;
        private string _lastNativeInvalidReason = string.Empty;

        public OpponentOutputs Outputs { get; } = new OpponentOutputs();
        public delegate bool TryGetCheckpointGapSec(int playerCarIdx, int targetCarIdx, out double signedGapSec);

        public OpponentsEngine()
        {
            _pitExitPredictor = new PitExitPredictor(Outputs.PitExit);
        }

        public void Reset()
        {
            _gateActive = false;
            _gateOpenedLogged = false;
            _pitExitWasRaceActive = false;
            _playerIdentityKey = string.Empty;
            _lastNativeInvalidLogUtc = DateTime.MinValue;
            _lastNativeInvalidReason = string.Empty;
            Outputs.Reset();
            _entityCache.Clear();
            _raceModel.Reset();
            _pitExitPredictor.Reset();
        }

        public void Update(GameData data, PluginManager pluginManager, bool isEligibleSession, bool isRaceSession, int completedLaps, double myPaceSec, double pitLossSec, bool pitTripActive, bool onPitRoad, double trackPct, double sessionTimeSec, double sessionTimeRemainingSec, bool debugEnabled, TryGetCheckpointGapSec tryGetCheckpointGapSec = null)
        {
            var _ = data;
            _tryGetCheckpointGapSec = tryGetCheckpointGapSec;

            if (!isEligibleSession)
            {
                if (_gateActive || _entityCache.Count > 0)
                {
                    Reset();
                }
                return;
            }

            if (!_gateActive)
            {
                _gateActive = true;
                if (!_gateOpenedLogged)
                {
                    SimHub.Logging.Current.Info("[LalaPlugin:Opponents] Opponents subsystem active (eligible live session).");
                    _gateOpenedLogged = true;
                }
            }

            var snapshot = NativeSnapshot.Build(pluginManager);
            if (!snapshot.IsValid)
            {
                Outputs.Reset();
                _raceModel.Reset();
                _pitExitPredictor.Reset();
                _pitExitWasRaceActive = false;
                LogNativeInvalid(snapshot.InvalidReason);
                return;
            }

            _playerIdentityKey = snapshot.PlayerIdentityKey;
            double validatedMyPace = SanitizePace(myPaceSec);
            _raceModel.Build(snapshot, validatedMyPace, _entityCache);
            PublishRaceOutputs(validatedMyPace);

            if (isRaceSession)
            {
                _pitExitPredictor.Update(_raceModel, _playerIdentityKey, pitLossSec, pitTripActive, onPitRoad, trackPct, completedLaps, sessionTimeSec, sessionTimeRemainingSec, debugEnabled);
                _pitExitWasRaceActive = true;
            }
            else if (_pitExitWasRaceActive)
            {
                _pitExitPredictor.Reset();
                _pitExitWasRaceActive = false;
            }
        }

        public static string MakeIdentityKey(string classColor, string carNumber)
        {
            string normalizedColor = NormalizeClassColor(classColor);
            string normalizedNumber = NormalizeCarNumber(carNumber);
            if (string.IsNullOrWhiteSpace(normalizedColor) || string.IsNullOrWhiteSpace(normalizedNumber))
            {
                return string.Empty;
            }

            return normalizedColor + ":" + normalizedNumber;
        }

        public bool TryGetPitExitSnapshot(out PitExitSnapshot snapshot)
        {
            return _pitExitPredictor.TryGetSnapshot(out snapshot);
        }

        public bool TryGetPitExitMathAudit(out string auditLine)
        {
            return _pitExitPredictor.TryBuildMathAudit(_raceModel, out auditLine);
        }

        public void NotifyPitExitLine(int completedLaps, double sessionTimeSec, double trackPct)
        {
            _pitExitPredictor.NotifyPitExitLine(completedLaps, sessionTimeSec, trackPct);
        }

        public bool TryGetPlayerRaceState(out int posClass, out int posOverall, out double gapToLeaderSec)
        {
            posClass = 0;
            posOverall = 0;
            gapToLeaderSec = double.NaN;

            if (!_raceModel.TryGetPlayerRow(out var player))
            {
                return false;
            }

            posClass = player.PositionInClass;
            posOverall = player.PositionOverall;
            gapToLeaderSec = _raceModel.ComputeGapToClassLeaderSec(player);
            return true;
        }

        private void PublishRaceOutputs(double myPaceSec)
        {
            Outputs.Ahead1.Reset();
            Outputs.Ahead2.Reset();
            Outputs.Behind1.Reset();
            Outputs.Behind2.Reset();

            PopulateTarget(Outputs.Ahead1, _raceModel.Ahead1, myPaceSec, true, true);
            PopulateTarget(Outputs.Ahead2, _raceModel.Ahead2, myPaceSec, true);
            PopulateTarget(Outputs.Behind1, _raceModel.Behind1, myPaceSec, false, true);
            PopulateTarget(Outputs.Behind2, _raceModel.Behind2, myPaceSec, false);

            Outputs.LeaderBlendedPaceSec = _raceModel.GetBlendedPaceForPosition(1);
            Outputs.P2BlendedPaceSec = _raceModel.GetBlendedPaceForPosition(2);

            var summaries = BuildSummaries(Outputs);
            Outputs.SummaryAhead = summaries.Ahead;
            Outputs.SummaryBehind = summaries.Behind;
            Outputs.SummaryAhead1 = summaries.Ahead1;
            Outputs.SummaryAhead2 = summaries.Ahead2;
            Outputs.SummaryBehind1 = summaries.Behind1;
            Outputs.SummaryBehind2 = summaries.Behind2;
        }

        private void PopulateTarget(OpponentTargetOutput target, NativeCarRow row, double myPaceSec, bool isAhead, bool preferCarSaGap = false)
        {
            if (target == null)
            {
                return;
            }

            target.Reset();
            if (row == null || string.IsNullOrWhiteSpace(row.IdentityKey))
            {
                return;
            }

            target.Name = row.Name ?? string.Empty;
            target.CarNumber = row.CarNumber ?? string.Empty;
            target.ClassColor = row.ClassColor ?? string.Empty;
            target.GapToPlayerSec = row.GapToPlayerSec;

            if (preferCarSaGap
                && _tryGetCheckpointGapSec != null
                && _raceModel.TryGetPlayerRow(out var playerRow)
                && playerRow != null
                && playerRow.CarIdx >= 0
                && row.CarIdx >= 0
                && _tryGetCheckpointGapSec(playerRow.CarIdx, row.CarIdx, out double signedGapSec)
                && !double.IsNaN(signedGapSec)
                && !double.IsInfinity(signedGapSec))
            {
                double absoluteGap = Math.Abs(signedGapSec);
                bool signMatchesSide = isAhead ? signedGapSec > 0.0 : signedGapSec < 0.0;
                if (signMatchesSide && absoluteGap > 0.0 && absoluteGap <= 30.0)
                {
                    target.GapToPlayerSec = absoluteGap;
                }
            }

            var entity = _entityCache.Touch(row.IdentityKey, row.Name, row.CarNumber, row.ClassColor);
            if (entity != null)
            {
                entity.IngestLapTimes(row.LastLapSec, row.BestLapSec, row.IsInPit);
                target.BlendedPaceSec = entity.GetBlendedPaceSec();
            }

            if (double.IsNaN(myPaceSec) || double.IsNaN(target.BlendedPaceSec) || target.BlendedPaceSec <= 0.0)
            {
                return;
            }

            double closingRate = isAhead
                ? target.BlendedPaceSec - myPaceSec
                : myPaceSec - target.BlendedPaceSec;

            target.PaceDeltaSecPerLap = closingRate;
            if (target.GapToPlayerSec > 0.0 && closingRate > 0.05)
            {
                double lapsToFight = target.GapToPlayerSec / closingRate;
                target.LapsToFight = lapsToFight > 999.0 ? 999.0 : lapsToFight;
            }
        }

        private void LogNativeInvalid(string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
            {
                reason = "native prerequisites missing";
            }

            bool reasonChanged = !string.Equals(reason, _lastNativeInvalidReason, StringComparison.Ordinal);
            bool cadenceOpen = (DateTime.UtcNow - _lastNativeInvalidLogUtc).TotalSeconds >= 5.0;
            if (reasonChanged || cadenceOpen)
            {
                SimHub.Logging.Current.Info("[LalaPlugin:Opponents] Native data unavailable -> outputs invalid (" + reason + ").");
                _lastNativeInvalidReason = reason;
                _lastNativeInvalidLogUtc = DateTime.UtcNow;
            }
        }

        private static OpponentSummaries BuildSummaries(OpponentOutputs outputs)
        {
            string ahead1 = BuildTargetSummary("A1", outputs.Ahead1, true);
            string ahead2 = BuildTargetSummary("A2", outputs.Ahead2, true);
            string behind1 = BuildTargetSummary("B1", outputs.Behind1, false);
            string behind2 = BuildTargetSummary("B2", outputs.Behind2, false);

            return new OpponentSummaries
            {
                Ahead = BuildSideSummary("Ahead", ahead1, ahead2),
                Behind = BuildSideSummary("Behind", behind1, behind2),
                Ahead1 = ahead1,
                Ahead2 = ahead2,
                Behind1 = behind1,
                Behind2 = behind2
            };
        }

        private static string BuildSideSummary(string label, params string[] slots)
        {
            var populated = (slots ?? new string[0]).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            if (populated.Count == 0)
            {
                return label + ": —";
            }

            return label + ":  " + string.Join(" | ", populated);
        }

        private static string BuildTargetSummary(string label, OpponentTargetOutput target, bool isAhead)
        {
            if (target == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(target.Name) && string.IsNullOrWhiteSpace(target.CarNumber))
            {
                return label + " —";
            }

            string ident = !string.IsNullOrWhiteSpace(target.CarNumber) ? "#" + target.CarNumber : target.Name;
            string gap = FormatGap(target.GapToPlayerSec, isAhead);
            string delta = FormatDelta(target.PaceDeltaSecPerLap);
            string lapsToFight = FormatLapsToFight(target.LapsToFight);

            return label + " " + ident + " " + gap + " " + delta + " LTF=" + lapsToFight;
        }

        private static string FormatGap(double gapSec, bool isAhead)
        {
            if (double.IsNaN(gapSec) || double.IsInfinity(gapSec) || gapSec <= 0.0)
            {
                return "—";
            }

            string signed = (isAhead ? "+" : "-") + gapSec.ToString("0.0", CultureInfo.InvariantCulture);
            return signed + "s";
        }

        private static string FormatDelta(double delta)
        {
            if (double.IsNaN(delta) || double.IsInfinity(delta))
            {
                return "Δ—";
            }

            return "Δ" + delta.ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + "s/L";
        }

        private static string FormatLapsToFight(double lapsToFight)
        {
            if (double.IsNaN(lapsToFight) || double.IsInfinity(lapsToFight) || lapsToFight <= 0.0)
            {
                return "—";
            }

            return lapsToFight.ToString("0.#", CultureInfo.InvariantCulture);
        }

        private static double SanitizePace(double paceSec)
        {
            if (paceSec <= 0.0 || double.IsNaN(paceSec) || double.IsInfinity(paceSec) || paceSec > 10000.0)
            {
                return double.NaN;
            }

            return paceSec;
        }

        private static string NormalizeCarNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            return value.Trim();
        }

        private static string NormalizeClassColor(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string text = raw.Trim();
            if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(2);
            }
            if (text.StartsWith("#", StringComparison.OrdinalIgnoreCase))
            {
                text = text.Substring(1);
            }

            if (int.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsedHex))
            {
                return "0x" + (parsedHex & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);
            }

            if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
            {
                return "0x" + (parsedInt & 0xFFFFFF).ToString("X6", CultureInfo.InvariantCulture);
            }

            return string.Empty;
        }

        private static int ReadInt(PluginManager pluginManager, string propertyName, int fallback)
        {
            try
            {
                return Convert.ToInt32(pluginManager?.GetPropertyValue(propertyName) ?? fallback, CultureInfo.InvariantCulture);
            }
            catch
            {
                return fallback;
            }
        }

        private static string ReadString(PluginManager pluginManager, string propertyName)
        {
            try
            {
                return Convert.ToString(pluginManager?.GetPropertyValue(propertyName), CultureInfo.InvariantCulture) ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        private static double ReadDouble(object raw)
        {
            if (raw == null)
            {
                return double.NaN;
            }

            try
            {
                if (raw is TimeSpan ts)
                {
                    return ts.TotalSeconds;
                }

                if (raw is IConvertible)
                {
                    return Convert.ToDouble(raw, CultureInfo.InvariantCulture);
                }

                string text = Convert.ToString(raw, CultureInfo.InvariantCulture);
                if (string.IsNullOrWhiteSpace(text))
                {
                    return double.NaN;
                }

                if (double.TryParse(text.Trim(), NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
                {
                    return parsed;
                }
            }
            catch
            {
            }

            return double.NaN;
        }

        private static float[] ReadFloatArray(PluginManager pluginManager, string propertyName)
        {
            try
            {
                return pluginManager?.GetPropertyValue(propertyName) as float[];
            }
            catch
            {
                return null;
            }
        }

        private static int[] ReadIntArray(PluginManager pluginManager, string propertyName)
        {
            try
            {
                return pluginManager?.GetPropertyValue(propertyName) as int[];
            }
            catch
            {
                return null;
            }
        }

        private static bool[] ReadBoolArray(PluginManager pluginManager, string propertyName)
        {
            try
            {
                return pluginManager?.GetPropertyValue(propertyName) as bool[];
            }
            catch
            {
                return null;
            }
        }

        private static bool IsInWorld(int trackSurface)
        {
            return trackSurface >= 0;
        }

        private class NativeSnapshot
        {
            public readonly List<NativeCarRow> Rows = new List<NativeCarRow>();
            public bool IsValid;
            public string InvalidReason;
            public int PlayerCarIdx;
            public string PlayerIdentityKey;
            public double PaceReferenceSec;

            public static NativeSnapshot Build(PluginManager pluginManager)
            {
                var snapshot = new NativeSnapshot();
                snapshot.PlayerCarIdx = ReadInt(pluginManager, "DataCorePlugin.GameRawData.Telemetry.PlayerCarIdx", -1);
                if (snapshot.PlayerCarIdx < 0)
                {
                    snapshot.IsValid = false;
                    snapshot.InvalidReason = "PlayerCarIdx unavailable";
                    return snapshot;
                }

                var carIdxLap = ReadIntArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxLap");
                var carIdxLapDist = ReadFloatArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxLapDistPct");
                var carIdxBestLap = ReadFloatArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxBestLapTime");
                var carIdxLastLap = ReadFloatArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxLastLapTime");
                var carIdxClassPos = ReadIntArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxClassPosition");
                var carIdxOnPitRoad = ReadBoolArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxOnPitRoad");
                var carIdxTrackSurface = ReadIntArray(pluginManager, "DataCorePlugin.GameRawData.Telemetry.CarIdxTrackSurface");

                if (carIdxLap == null || carIdxLapDist == null)
                {
                    snapshot.IsValid = false;
                    snapshot.InvalidReason = "CarIdxLap/CarIdxLapDistPct unavailable";
                    return snapshot;
                }

                var seenCarIdx = new HashSet<int>();

                for (int i = 1; i <= 64; i++)
                {
                    string basePath = "DataCorePlugin.GameRawData.SessionData.DriverInfo.Drivers" + i.ToString("00", CultureInfo.InvariantCulture);
                    int idx = ReadInt(pluginManager, basePath + ".CarIdx", int.MinValue);
                    if (idx == int.MinValue || idx < 0 || seenCarIdx.Contains(idx))
                    {
                        continue;
                    }

                    var row = BuildRowFromDriver(pluginManager, basePath, idx, carIdxLap, carIdxLapDist, carIdxBestLap, carIdxLastLap, carIdxClassPos, carIdxOnPitRoad, carIdxTrackSurface);
                    if (row != null)
                    {
                        seenCarIdx.Add(idx);
                        snapshot.Rows.Add(row);
                    }
                }

                for (int i = 0; i < 64; i++)
                {
                    string basePath = "DataCorePlugin.GameRawData.SessionData.DriverInfo.CompetingDrivers[" + i.ToString(CultureInfo.InvariantCulture) + "]";
                    int idx = ReadInt(pluginManager, basePath + ".CarIdx", int.MinValue);
                    if (idx == int.MinValue)
                    {
                        break;
                    }

                    if (idx < 0 || seenCarIdx.Contains(idx))
                    {
                        continue;
                    }

                    var row = BuildRowFromCompeting(pluginManager, basePath, idx, carIdxLap, carIdxLapDist, carIdxBestLap, carIdxLastLap, carIdxClassPos, carIdxOnPitRoad, carIdxTrackSurface);
                    if (row != null)
                    {
                        seenCarIdx.Add(idx);
                        snapshot.Rows.Add(row);
                    }
                }

                var player = snapshot.Rows.FirstOrDefault(r => r.CarIdx == snapshot.PlayerCarIdx);
                if (player == null)
                {
                    snapshot.IsValid = false;
                    snapshot.InvalidReason = "player not found in DriverInfo";
                    return snapshot;
                }

                snapshot.PlayerIdentityKey = player.IdentityKey;
                if (string.IsNullOrWhiteSpace(snapshot.PlayerIdentityKey))
                {
                    snapshot.IsValid = false;
                    snapshot.InvalidReason = "player identity incomplete";
                    return snapshot;
                }

                snapshot.PaceReferenceSec = ValidLapTime(player.BestLapSec) ? player.BestLapSec : (ValidLapTime(player.LastLapSec) ? player.LastLapSec : 120.0);
                snapshot.IsValid = true;
                return snapshot;
            }

            private static NativeCarRow BuildRowFromDriver(PluginManager pluginManager, string basePath, int carIdx,
                int[] carIdxLap, float[] carIdxLapDist, float[] carIdxBestLap, float[] carIdxLastLap, int[] carIdxClassPos, bool[] carIdxOnPitRoad, int[] carIdxTrackSurface)
            {
                string name = ReadString(pluginManager, basePath + ".UserName");
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = ReadString(pluginManager, basePath + ".AbbrevName");
                }

                string number = ReadString(pluginManager, basePath + ".CarNumber");
                if (string.IsNullOrWhiteSpace(number))
                {
                    int raw = ReadInt(pluginManager, basePath + ".CarNumberRaw", int.MinValue);
                    if (raw != int.MinValue)
                    {
                        number = raw.ToString(CultureInfo.InvariantCulture);
                    }
                }

                string classColor = NormalizeClassColor(ReadString(pluginManager, basePath + ".CarClassColor"));
                return BuildTelemetryRow(carIdx, name, number, classColor, carIdxLap, carIdxLapDist, carIdxBestLap, carIdxLastLap, carIdxClassPos, carIdxOnPitRoad, carIdxTrackSurface);
            }

            private static NativeCarRow BuildRowFromCompeting(PluginManager pluginManager, string basePath, int carIdx,
                int[] carIdxLap, float[] carIdxLapDist, float[] carIdxBestLap, float[] carIdxLastLap, int[] carIdxClassPos, bool[] carIdxOnPitRoad, int[] carIdxTrackSurface)
            {
                string name = ReadString(pluginManager, basePath + ".UserName");
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = ReadString(pluginManager, basePath + ".TeamName");
                }

                string number = ReadString(pluginManager, basePath + ".CarNumber");
                string classColor = NormalizeClassColor(ReadString(pluginManager, basePath + ".CarClassColor"));
                return BuildTelemetryRow(carIdx, name, number, classColor, carIdxLap, carIdxLapDist, carIdxBestLap, carIdxLastLap, carIdxClassPos, carIdxOnPitRoad, carIdxTrackSurface);
            }

            private static NativeCarRow BuildTelemetryRow(int carIdx, string name, string number, string classColor,
                int[] carIdxLap, float[] carIdxLapDist, float[] carIdxBestLap, float[] carIdxLastLap, int[] carIdxClassPos, bool[] carIdxOnPitRoad, int[] carIdxTrackSurface)
            {
                if (carIdx < 0 || carIdx >= carIdxLap.Length || carIdx >= carIdxLapDist.Length)
                {
                    return null;
                }

                double lapDist = carIdxLapDist[carIdx];
                bool validLapDist = !float.IsNaN((float)lapDist) && !float.IsInfinity((float)lapDist) && lapDist >= 0.0 && lapDist <= 1.0;
                int lap = carIdxLap[carIdx];
                int trackSurface = (carIdxTrackSurface != null && carIdx < carIdxTrackSurface.Length) ? carIdxTrackSurface[carIdx] : -1;
                bool inWorld = IsInWorld(trackSurface);
                bool onPitRoad = carIdxOnPitRoad != null && carIdx < carIdxOnPitRoad.Length && carIdxOnPitRoad[carIdx];

                double bestLap = (carIdxBestLap != null && carIdx < carIdxBestLap.Length) ? carIdxBestLap[carIdx] : float.NaN;
                double lastLap = (carIdxLastLap != null && carIdx < carIdxLastLap.Length) ? carIdxLastLap[carIdx] : float.NaN;

                int classPos = (carIdxClassPos != null && carIdx < carIdxClassPos.Length) ? carIdxClassPos[carIdx] : 0;

                string carNumber = NormalizeCarNumber(number);
                string identityKey = MakeIdentityKey(classColor, carNumber);

                return new NativeCarRow
                {
                    CarIdx = carIdx,
                    Name = name ?? string.Empty,
                    CarNumber = carNumber,
                    ClassColor = classColor,
                    IdentityKey = identityKey,
                    Lap = lap,
                    LapDistPct = validLapDist ? lapDist : double.NaN,
                    HasValidLapDist = validLapDist,
                    IsConnected = inWorld,
                    IsInPit = onPitRoad,
                    BestLapSec = ValidLapTime(bestLap) ? bestLap : double.NaN,
                    LastLapSec = ValidLapTime(lastLap) ? lastLap : double.NaN,
                    PositionInClass = classPos
                };
            }

            private static bool ValidLapTime(double value)
            {
                return !double.IsNaN(value) && !double.IsInfinity(value) && value > 0.0 && value < 10000.0;
            }
        }

        private class NativeRaceModel
        {
            private readonly List<NativeCarRow> _rows = new List<NativeCarRow>();
            private bool _usingClassPosition;
            private double _paceReferenceSec = 120.0;

            public NativeCarRow Player { get; private set; }
            public NativeCarRow Ahead1 { get; private set; }
            public NativeCarRow Ahead2 { get; private set; }
            public NativeCarRow Behind1 { get; private set; }
            public NativeCarRow Behind2 { get; private set; }

            public IReadOnlyList<NativeCarRow> Rows => _rows;

            public void Reset()
            {
                _rows.Clear();
                Player = null;
                Ahead1 = null;
                Ahead2 = null;
                Behind1 = null;
                Behind2 = null;
                _usingClassPosition = false;
                _paceReferenceSec = 120.0;
            }

            public void Build(NativeSnapshot snapshot, double myPaceSec, EntityCache cache)
            {
                Reset();
                _rows.AddRange(snapshot.Rows.Where(r => r != null && r.IsConnected && !string.IsNullOrWhiteSpace(r.IdentityKey)));
                Player = _rows.FirstOrDefault(r => string.Equals(r.IdentityKey, snapshot.PlayerIdentityKey, StringComparison.Ordinal));
                if (Player == null)
                {
                    return;
                }

                _paceReferenceSec = !double.IsNaN(myPaceSec) ? myPaceSec : snapshot.PaceReferenceSec;
                if (double.IsNaN(_paceReferenceSec) || _paceReferenceSec <= 0.0)
                {
                    _paceReferenceSec = 120.0;
                }

                foreach (var row in _rows)
                {
                    var entity = cache.Touch(row.IdentityKey, row.Name, row.CarNumber, row.ClassColor);
                    if (entity != null)
                    {
                        entity.IngestLapTimes(row.LastLapSec, row.BestLapSec, row.IsInPit);
                        row.BlendedPaceSec = entity.GetBlendedPaceSec();
                    }
                }

                var overallOrdered = _rows
                    .Where(r => r.HasValidLapDist)
                    .OrderByDescending(r => r.Lap)
                    .ThenByDescending(r => r.LapDistPct)
                    .ThenBy(r => r.CarIdx)
                    .ToList();
                for (int i = 0; i < overallOrdered.Count; i++)
                {
                    overallOrdered[i].PositionOverall = i + 1;
                }

                var sameClass = _rows.Where(r => string.Equals(r.ClassColor, Player.ClassColor, StringComparison.Ordinal)).ToList();
                if (sameClass.Count == 0)
                {
                    return;
                }

                _usingClassPosition = CanUseClassPosition(sameClass);
                if (_usingClassPosition)
                {
                    foreach (var row in sameClass)
                    {
                        row.SortKey = row.PositionInClass > 0 ? row.PositionInClass : double.MaxValue;
                    }
                    sameClass = sameClass.OrderBy(r => r.SortKey).ToList();
                }
                else
                {
                    sameClass = sameClass
                        .Where(r => r.HasValidLapDist)
                        .OrderByDescending(r => r.Lap)
                        .ThenByDescending(r => r.LapDistPct)
                        .ThenBy(r => r.CarIdx)
                        .ToList();

                    for (int i = 0; i < sameClass.Count; i++)
                    {
                        sameClass[i].PositionInClass = i + 1;
                    }
                }

                int playerIndex = sameClass.FindIndex(r => string.Equals(r.IdentityKey, Player.IdentityKey, StringComparison.Ordinal));
                if (playerIndex < 0)
                {
                    return;
                }

                Ahead1 = playerIndex - 1 >= 0 ? sameClass[playerIndex - 1] : null;
                Ahead2 = playerIndex - 2 >= 0 ? sameClass[playerIndex - 2] : null;
                Behind1 = playerIndex + 1 < sameClass.Count ? sameClass[playerIndex + 1] : null;
                Behind2 = playerIndex + 2 < sameClass.Count ? sameClass[playerIndex + 2] : null;

                ApplyGapToPlayer(Player, Ahead1);
                ApplyGapToPlayer(Player, Ahead2);
                ApplyGapToPlayer(Player, Behind1);
                ApplyGapToPlayer(Player, Behind2);
            }

            public bool TryGetPlayerRow(out NativeCarRow player)
            {
                player = Player;
                return player != null;
            }

            public double GetBlendedPaceForPosition(int positionInClass)
            {
                if (positionInClass <= 0 || Player == null)
                {
                    return double.NaN;
                }

                var row = _rows.FirstOrDefault(r => string.Equals(r.ClassColor, Player.ClassColor, StringComparison.Ordinal) && r.PositionInClass == positionInClass);
                return row != null ? row.BlendedPaceSec : double.NaN;
            }

            public double ComputeGapToClassLeaderSec(NativeCarRow player)
            {
                if (player == null)
                {
                    return double.NaN;
                }

                var leader = _rows.Where(r => string.Equals(r.ClassColor, player.ClassColor, StringComparison.Ordinal))
                    .OrderBy(r => r.PositionInClass <= 0 ? int.MaxValue : r.PositionInClass)
                    .FirstOrDefault();

                if (leader == null)
                {
                    return double.NaN;
                }

                return Math.Abs(ComputeProgressDeltaLaps(player, leader) * _paceReferenceSec);
            }

            private static bool CanUseClassPosition(List<NativeCarRow> sameClass)
            {
                if (sameClass == null || sameClass.Count == 0)
                {
                    return false;
                }

                int valid = sameClass.Count(r => r.PositionInClass > 0);
                return valid >= Math.Max(2, sameClass.Count / 2);
            }

            private void ApplyGapToPlayer(NativeCarRow player, NativeCarRow target)
            {
                if (player == null || target == null)
                {
                    return;
                }

                double deltaLaps = ComputeProgressDeltaLaps(player, target);
                target.GapToPlayerSec = Math.Abs(deltaLaps * _paceReferenceSec);
            }

            public static double ComputeProgressDeltaLaps(NativeCarRow left, NativeCarRow right)
            {
                if (left == null || right == null || !left.HasValidLapDist || !right.HasValidLapDist)
                {
                    return double.NaN;
                }

                return (right.Lap + right.LapDistPct) - (left.Lap + left.LapDistPct);
            }
        }

        private class PitExitPredictor
        {
            private readonly PitExitOutput _output;
            private bool _lastValid;
            private int _lastPredictedPos;
            private DateTime _lastPredictedLogUtc = DateTime.MinValue;
            private bool _lastPitTripActive;
            private bool _lastOnPitRoad;
            private bool _hasSnapshot;
            private PitExitSnapshot _snapshot;
            private int _lastSeg = -1;
            private double _lastTrackPct = double.NaN;
            private int _lastCompletedLaps = -1;
            private bool _pitTripLockActive;
            private double _pitEntryProgressLaps = double.NaN;
            private double _pitLossLockedSec;
            private bool _pendingSettledPitOut;
            private int _pendingSettledPitOutLap = -1;
            private double _pitExitSessionTimeSec = double.NaN;
            private double _pitExitTrackPct = double.NaN;

            public PitExitPredictor(PitExitOutput output)
            {
                _output = output;
            }

            public void Reset()
            {
                _lastValid = false;
                _lastPredictedPos = 0;
                _lastPredictedLogUtc = DateTime.MinValue;
                _lastPitTripActive = false;
                _lastOnPitRoad = false;
                _hasSnapshot = false;
                _snapshot = new PitExitSnapshot();
                _lastSeg = -1;
                _lastTrackPct = double.NaN;
                _lastCompletedLaps = -1;
                _pitTripLockActive = false;
                _pitEntryProgressLaps = double.NaN;
                _pitLossLockedSec = 0.0;
                _pendingSettledPitOut = false;
                _pendingSettledPitOutLap = -1;
                _pitExitSessionTimeSec = double.NaN;
                _pitExitTrackPct = double.NaN;
                _output.Reset();
            }

            public void Update(NativeRaceModel raceModel, string playerIdentityKey, double pitLossSec, bool pitTripActive, bool onPitRoad, double trackPct, int completedLaps, double sessionTimeSec, double sessionTimeRemainingSec, bool debugEnabled)
            {
                bool allowLogs = true;
                bool inFinalSuppressionWindow = !double.IsNaN(sessionTimeRemainingSec)
                    && !double.IsInfinity(sessionTimeRemainingSec)
                    && sessionTimeRemainingSec <= 120.0;

                if (inFinalSuppressionWindow && !_pendingSettledPitOut)
                {
                    return;
                }

                if (inFinalSuppressionWindow && _pendingSettledPitOut)
                {
                    _pendingSettledPitOut = false;
                    _pendingSettledPitOutLap = -1;
                    _pitExitSessionTimeSec = double.NaN;
                    _pitExitTrackPct = double.NaN;
                }

                bool hasTrackPct = !double.IsNaN(trackPct) && !double.IsInfinity(trackPct);
                bool lapCrossed = _lastCompletedLaps >= 0 && completedLaps > _lastCompletedLaps;
                if (lapCrossed || (hasTrackPct && _lastTrackPct > 0.80 && trackPct < 0.20))
                {
                    _lastSeg = -1;
                }

                if (!onPitRoad && hasTrackPct)
                {
                    int seg = (int)(trackPct * 4.0);
                    if (seg < 0) seg = 0;
                    if (seg > 3) seg = 3;
                    if (seg == _lastSeg)
                    {
                        _lastTrackPct = trackPct;
                        _lastCompletedLaps = completedLaps;
                        return;
                    }

                    _lastSeg = seg;
                }

                _lastTrackPct = trackPct;
                _lastCompletedLaps = completedLaps;

                if (!raceModel.TryGetPlayerRow(out var player))
                {
                    SetInvalid(allowLogs);
                    return;
                }

                var classRows = raceModel.Rows.Where(r => string.Equals(r.ClassColor, player.ClassColor, StringComparison.Ordinal) && r.IsConnected && r.HasValidLapDist).ToList();
                if (classRows.Count == 0 || !player.HasValidLapDist)
                {
                    SetInvalid(allowLogs);
                    return;
                }

                double playerProgress = player.Lap + player.LapDistPct;
                double pitLoss = (pitLossSec > 0.0 && !double.IsNaN(pitLossSec) && !double.IsInfinity(pitLossSec)) ? pitLossSec : 0.0;
                double paceRef = !double.IsNaN(player.BestLapSec) ? player.BestLapSec : (!double.IsNaN(player.LastLapSec) ? player.LastLapSec : 120.0);
                if (paceRef <= 0.0 || double.IsNaN(paceRef) || double.IsInfinity(paceRef))
                {
                    paceRef = 120.0;
                }

                bool pitTripStarted = pitTripActive && !_lastPitTripActive;
                bool pitTripEnded = !pitTripActive && _lastPitTripActive;

                if (pitTripStarted)
                {
                    _pitTripLockActive = true;
                    _pitEntryProgressLaps = playerProgress;
                    _pitLossLockedSec = pitLoss;
                }
                else if (pitTripEnded)
                {
                    _pitTripLockActive = false;
                    _pitEntryProgressLaps = double.NaN;
                    _pitLossLockedSec = 0.0;
                }

                double progressUsed = (_pitTripLockActive && !double.IsNaN(_pitEntryProgressLaps)) ? _pitEntryProgressLaps : playerProgress;
                double pitLossUsed = _pitTripLockActive ? _pitLossLockedSec : pitLoss;
                double playerPredictedProgressAfterPit = progressUsed - (pitLossUsed / paceRef);

                int carsAheadAfterPit = 0;
                double nearestAheadDelta = double.NegativeInfinity;
                double nearestBehindDelta = double.PositiveInfinity;
                NativeCarRow nearestAhead = null;
                NativeCarRow nearestBehind = null;

                foreach (var row in classRows)
                {
                    if (string.Equals(row.IdentityKey, playerIdentityKey, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    double candidateProgress = row.Lap + row.LapDistPct;
                    double delta = candidateProgress - playerPredictedProgressAfterPit;
                    if (double.IsNaN(delta) || double.IsInfinity(delta))
                    {
                        continue;
                    }

                    if (delta > 0.0)
                    {
                        carsAheadAfterPit++;
                        if (delta < nearestBehindDelta)
                        {
                            nearestBehindDelta = delta;
                            nearestBehind = row;
                        }
                    }
                    else if (delta < 0.0)
                    {
                        if (delta > nearestAheadDelta)
                        {
                            nearestAheadDelta = delta;
                            nearestAhead = row;
                        }
                    }
                }

                int predictedPos = 1 + carsAheadAfterPit;

                if (nearestBehind != null)
                {
                    _output.AheadName = nearestBehind.Name ?? string.Empty;
                    _output.AheadCarNumber = nearestBehind.CarNumber ?? string.Empty;
                    _output.AheadClassColor = nearestBehind.ClassColor ?? string.Empty;
                    _output.AheadGapSec = Math.Abs(nearestBehindDelta * paceRef);
                }
                else
                {
                    _output.AheadName = string.Empty;
                    _output.AheadCarNumber = string.Empty;
                    _output.AheadClassColor = string.Empty;
                    _output.AheadGapSec = 0.0;
                }

                if (nearestAhead != null)
                {
                    _output.BehindName = nearestAhead.Name ?? string.Empty;
                    _output.BehindCarNumber = nearestAhead.CarNumber ?? string.Empty;
                    _output.BehindClassColor = nearestAhead.ClassColor ?? string.Empty;
                    _output.BehindGapSec = Math.Abs(nearestAheadDelta * paceRef);
                }
                else
                {
                    _output.BehindName = string.Empty;
                    _output.BehindCarNumber = string.Empty;
                    _output.BehindClassColor = string.Empty;
                    _output.BehindGapSec = 0.0;
                }

                _output.Valid = true;
                _output.PredictedPositionInClass = predictedPos;
                _output.CarsAheadAfterPitCount = carsAheadAfterPit;
                _output.Summary = "PitExit: P" + predictedPos.ToString(CultureInfo.InvariantCulture) + " after stop (A=" +
                    (string.IsNullOrWhiteSpace(_output.AheadCarNumber) ? "-" : _output.AheadCarNumber + "+" + _output.AheadGapSec.ToString("F1", CultureInfo.InvariantCulture) + "s") +
                    ", B=" +
                    (string.IsNullOrWhiteSpace(_output.BehindCarNumber) ? "-" : _output.BehindCarNumber + "+" + _output.BehindGapSec.ToString("F1", CultureInfo.InvariantCulture) + "s") +
                    ", loss=" + pitLossUsed.ToString("F1", CultureInfo.InvariantCulture) + "s)";

                _snapshot = new PitExitSnapshot
                {
                    Valid = true,
                    PlayerIdentityKey = playerIdentityKey,
                    PlayerPositionInClass = player.PositionInClass,
                    PlayerPositionOverall = player.PositionOverall,
                    PlayerGapToLeader = double.NaN,
                    PitLossSec = pitLoss,
                    PredictedPositionInClass = predictedPos,
                    CarsAheadAfterPit = carsAheadAfterPit,
                    AheadName = _output.AheadName,
                    AheadCarNumber = _output.AheadCarNumber,
                    AheadClassColor = _output.AheadClassColor,
                    AheadGapSec = _output.AheadGapSec,
                    BehindName = _output.BehindName,
                    BehindCarNumber = _output.BehindCarNumber,
                    BehindClassColor = _output.BehindClassColor,
                    BehindGapSec = _output.BehindGapSec,
                    GapToLeaderLiveSec = double.NaN,
                    GapToLeaderUsedSec = double.NaN,
                    PitEntryGapToLeaderSec = _pitEntryProgressLaps,
                    PitLossLiveSec = pitLoss,
                    PitLossUsedSec = pitLossUsed,
                    PredGapAfterPitSec = playerPredictedProgressAfterPit,
                    PitTripLockActive = _pitTripLockActive
                };
                _hasSnapshot = true;

                if (_output.Valid != _lastValid)
                {
                    SimHub.Logging.Current.Info("[LalaPlugin:PitExit] Predictor valid -> true (pitLoss=" + pitLoss.ToString("F1", CultureInfo.InvariantCulture) + "s)");
                }
                else if (_output.Valid && predictedPos != _lastPredictedPos && debugEnabled)
                {
                    bool largeChange = Math.Abs(predictedPos - _lastPredictedPos) >= 2;
                    bool timeElapsed = (DateTime.UtcNow - _lastPredictedLogUtc).TotalSeconds >= 2.0;
                    bool pitTripChanged = pitTripActive != _lastPitTripActive;
                    bool onPitRoadChanged = onPitRoad != _lastOnPitRoad;

                    if (largeChange || timeElapsed || pitTripChanged || onPitRoadChanged)
                    {
                        SimHub.Logging.Current.Info("[LalaPlugin:PitExit] Predicted class position changed -> P" + predictedPos.ToString(CultureInfo.InvariantCulture) + " (ahead=" + carsAheadAfterPit.ToString(CultureInfo.InvariantCulture) + ")");
                        _lastPredictedLogUtc = DateTime.UtcNow;
                    }
                }

                _lastValid = _output.Valid;
                _lastPredictedPos = predictedPos;
                _lastPitTripActive = pitTripActive;
                _lastOnPitRoad = onPitRoad;
            }

            public bool TryGetSnapshot(out PitExitSnapshot snapshot)
            {
                snapshot = _snapshot;
                return _hasSnapshot;
            }

            public void NotifyPitExitLine(int completedLaps, double sessionTimeSec, double trackPct)
            {
                _pendingSettledPitOut = true;
                _pendingSettledPitOutLap = completedLaps;
                _pitExitSessionTimeSec = sessionTimeSec;
                _pitExitTrackPct = trackPct;
            }

            public bool TryBuildMathAudit(NativeRaceModel raceModel, out string auditLine)
            {
                auditLine = null;
                if (!_hasSnapshot || !raceModel.TryGetPlayerRow(out var player))
                {
                    return false;
                }

                auditLine = "Math audit: native progress model active (playerProgressLock=" +
                    (_pitTripLockActive && !double.IsNaN(_pitEntryProgressLaps)
                        ? _pitEntryProgressLaps.ToString("F3", CultureInfo.InvariantCulture)
                        : "off") +
                    ", pitLossUsed=" + _snapshot.PitLossUsedSec.ToString("F1", CultureInfo.InvariantCulture) + "s)";
                return true;
            }

            private void SetInvalid(bool allowLogs)
            {
                if (_lastValid && allowLogs)
                {
                    SimHub.Logging.Current.Info("[LalaPlugin:PitExit] Predictor valid -> false");
                }

                _output.Reset();
                _lastValid = false;
                _lastPredictedPos = 0;
                _hasSnapshot = false;
            }
        }

        private class EntityCache
        {
            private readonly Dictionary<string, OpponentEntity> _entities = new Dictionary<string, OpponentEntity>();
            public int Count => _entities.Count;
            public void Clear() { _entities.Clear(); }

            public OpponentEntity Touch(string identityKey, string name, string carNumber, string classColor)
            {
                if (string.IsNullOrWhiteSpace(identityKey)) return null;
                if (!_entities.TryGetValue(identityKey, out var entity))
                {
                    entity = new OpponentEntity(identityKey);
                    _entities[identityKey] = entity;
                }

                entity.UpdateMetadata(name, carNumber, classColor);
                return entity;
            }
        }

        private class OpponentEntity
        {
            private const int PaceWindowSize = 5;
            private readonly double[] _recentLaps = new double[PaceWindowSize];
            private int _lapCount;
            private int _lapIndex;

            public OpponentEntity(string identityKey)
            {
                IdentityKey = identityKey;
            }

            public string IdentityKey { get; private set; }
            public string Name { get; private set; } = string.Empty;
            public string CarNumber { get; private set; } = string.Empty;
            public string ClassColor { get; private set; } = string.Empty;
            public double BestLapSec { get; private set; } = double.NaN;

            public void UpdateMetadata(string name, string carNumber, string classColor)
            {
                if (!string.IsNullOrWhiteSpace(name)) Name = name;
                if (!string.IsNullOrWhiteSpace(carNumber)) CarNumber = carNumber;
                if (!string.IsNullOrWhiteSpace(classColor)) ClassColor = classColor;
            }

            public void IngestLapTimes(double lastLapSec, double bestLapSec, bool isInPit)
            {
                if (!double.IsNaN(bestLapSec) && !double.IsInfinity(bestLapSec) && bestLapSec > 0.0 && bestLapSec < 10000.0)
                {
                    if (double.IsNaN(BestLapSec) || bestLapSec < BestLapSec)
                    {
                        BestLapSec = bestLapSec;
                    }
                }

                if (isInPit || double.IsNaN(lastLapSec) || double.IsInfinity(lastLapSec) || lastLapSec <= 0.0 || lastLapSec >= 10000.0)
                {
                    return;
                }

                _recentLaps[_lapIndex] = lastLapSec;
                _lapIndex = (_lapIndex + 1) % PaceWindowSize;
                if (_lapCount < PaceWindowSize) _lapCount++;
            }

            public double GetBlendedPaceSec()
            {
                double recent = GetRecentAverage();
                bool hasRecent = !double.IsNaN(recent) && recent > 0.0;
                bool hasBest = !double.IsNaN(BestLapSec) && BestLapSec > 0.0;
                double bestAdjusted = hasBest ? BestLapSec * 1.01 : double.NaN;

                if (hasRecent && hasBest) return (0.70 * recent) + (0.30 * bestAdjusted);
                if (hasRecent) return recent;
                if (hasBest) return bestAdjusted;
                return double.NaN;
            }

            private double GetRecentAverage()
            {
                if (_lapCount == 0) return double.NaN;
                double sum = 0.0;
                for (int i = 0; i < _lapCount; i++) sum += _recentLaps[i];
                return sum / _lapCount;
            }
        }

        private class NativeCarRow
        {
            public int CarIdx;
            public string IdentityKey;
            public string Name;
            public string CarNumber;
            public string ClassColor;
            public int PositionOverall;
            public int PositionInClass;
            public double GapToPlayerSec;
            public bool IsConnected;
            public bool IsInPit;
            public int Lap;
            public double LapDistPct;
            public bool HasValidLapDist;
            public double BestLapSec;
            public double LastLapSec;
            public double SortKey;
            public double BlendedPaceSec;
        }

        public class OpponentOutputs
        {
            public OpponentOutputs()
            {
                PitExit = new PitExitOutput();
            }

            public OpponentTargetOutput Ahead1 { get; } = new OpponentTargetOutput();
            public OpponentTargetOutput Ahead2 { get; } = new OpponentTargetOutput();
            public OpponentTargetOutput Behind1 { get; } = new OpponentTargetOutput();
            public OpponentTargetOutput Behind2 { get; } = new OpponentTargetOutput();
            public OpponentTargetOutput Leader { get; } = new OpponentTargetOutput();
            public OpponentTargetOutput P2 { get; } = new OpponentTargetOutput();
            public PitExitOutput PitExit { get; }
            public string SummaryAhead { get; set; } = string.Empty;
            public string SummaryBehind { get; set; } = string.Empty;
            public string SummaryAhead1 { get; set; } = string.Empty;
            public string SummaryAhead2 { get; set; } = string.Empty;
            public string SummaryBehind1 { get; set; } = string.Empty;
            public string SummaryBehind2 { get; set; } = string.Empty;
            public double LeaderBlendedPaceSec { get; set; } = double.NaN;
            public double P2BlendedPaceSec { get; set; } = double.NaN;

            public void Reset()
            {
                Ahead1.Reset();
                Ahead2.Reset();
                Behind1.Reset();
                Behind2.Reset();
                Leader.Reset();
                P2.Reset();
                PitExit.Reset();
                SummaryAhead = string.Empty;
                SummaryBehind = string.Empty;
                SummaryAhead1 = string.Empty;
                SummaryAhead2 = string.Empty;
                SummaryBehind1 = string.Empty;
                SummaryBehind2 = string.Empty;
                LeaderBlendedPaceSec = double.NaN;
                P2BlendedPaceSec = double.NaN;
            }
        }

        public class OpponentSummaries
        {
            public string Ahead { get; set; } = string.Empty;
            public string Behind { get; set; } = string.Empty;
            public string Ahead1 { get; set; } = string.Empty;
            public string Ahead2 { get; set; } = string.Empty;
            public string Behind1 { get; set; } = string.Empty;
            public string Behind2 { get; set; } = string.Empty;
        }

        public class OpponentTargetOutput
        {
            public string Name { get; set; } = string.Empty;
            public string CarNumber { get; set; } = string.Empty;
            public string ClassColor { get; set; } = string.Empty;
            public double GapToPlayerSec { get; set; } = 0.0;
            public double BlendedPaceSec { get; set; } = double.NaN;
            public double PaceDeltaSecPerLap { get; set; } = double.NaN;
            public double LapsToFight { get; set; } = double.NaN;

            public void Reset()
            {
                Name = string.Empty;
                CarNumber = string.Empty;
                ClassColor = string.Empty;
                GapToPlayerSec = 0.0;
                BlendedPaceSec = double.NaN;
                PaceDeltaSecPerLap = double.NaN;
                LapsToFight = double.NaN;
            }
        }

        public class PitExitSnapshot
        {
            public bool Valid { get; set; }
            public string PlayerIdentityKey { get; set; }
            public int PlayerPositionInClass { get; set; }
            public int PlayerPositionOverall { get; set; }
            public double PlayerGapToLeader { get; set; }
            public double PitLossSec { get; set; }
            public double GapToLeaderLiveSec { get; set; }
            public double GapToLeaderUsedSec { get; set; }
            public double PitEntryGapToLeaderSec { get; set; }
            public double PitLossLiveSec { get; set; }
            public double PitLossUsedSec { get; set; }
            public double PredGapAfterPitSec { get; set; }
            public bool PitTripLockActive { get; set; }
            public int PredictedPositionInClass { get; set; }
            public int CarsAheadAfterPit { get; set; }
            public string AheadName { get; set; } = string.Empty;
            public string AheadCarNumber { get; set; } = string.Empty;
            public string AheadClassColor { get; set; } = string.Empty;
            public double AheadGapSec { get; set; }
            public string BehindName { get; set; } = string.Empty;
            public string BehindCarNumber { get; set; } = string.Empty;
            public string BehindClassColor { get; set; } = string.Empty;
            public double BehindGapSec { get; set; }
        }

        public class PitExitOutput
        {
            public bool Valid { get; set; }
            public int PredictedPositionInClass { get; set; }
            public int CarsAheadAfterPitCount { get; set; }
            public string Summary { get; set; } = string.Empty;
            public string AheadName { get; set; } = string.Empty;
            public string AheadCarNumber { get; set; } = string.Empty;
            public string AheadClassColor { get; set; } = string.Empty;
            public double AheadGapSec { get; set; }
            public string BehindName { get; set; } = string.Empty;
            public string BehindCarNumber { get; set; } = string.Empty;
            public string BehindClassColor { get; set; } = string.Empty;
            public double BehindGapSec { get; set; }

            public void Reset()
            {
                Valid = false;
                PredictedPositionInClass = 0;
                CarsAheadAfterPitCount = 0;
                Summary = string.Empty;
                AheadName = string.Empty;
                AheadCarNumber = string.Empty;
                AheadClassColor = string.Empty;
                AheadGapSec = 0.0;
                BehindName = string.Empty;
                BehindCarNumber = string.Empty;
                BehindClassColor = string.Empty;
                BehindGapSec = 0.0;
            }
        }
    }
}
