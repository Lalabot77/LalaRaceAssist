using System;
using System.Collections.Generic;
using SimHub.Plugins;
using LaunchPlugin;

namespace LaunchPlugin.Messaging
{
    public interface ISignalProvider
    {
        bool TryGet<T>(string signalId, out T value);
    }

    public class SignalProvider : ISignalProvider
    {
        private readonly PluginManager _pluginManager;
        private readonly LalaLaunch _plugin;
        private readonly Dictionary<string, Func<object>> _accessors;
        private readonly HashSet<string> _legacyExtraSignalWarned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public SignalProvider(PluginManager pluginManager, LalaLaunch plugin)
        {
            _pluginManager = pluginManager;
            _plugin = plugin;
            _accessors = BuildAccessors();
        }

        public bool TryGet<T>(string signalId, out T value)
        {
            value = default;

            if (string.IsNullOrWhiteSpace(signalId)) return false;
            if (!_accessors.TryGetValue(signalId, out var getter)) return false;

            try
            {
                var raw = getter?.Invoke();
                if (raw == null) return false;

                if (raw is T direct)
                {
                    value = direct;
                    return true;
                }

                try
                {
                    value = (T)Convert.ChangeType(raw, typeof(T));
                    return true;
                }
                catch
                {
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        private Dictionary<string, Func<object>> BuildAccessors()
        {
            return new Dictionary<string, Func<object>>(StringComparer.OrdinalIgnoreCase)
            {
                // Fuel signals (PluginCalc)
                { "FuelDeltaL_Current", () => _plugin?.Fuel_Delta_LitresCurrent },
                { "FuelLapsRemaining", () => _plugin?.LiveLapsRemainingInRace },
                { "FuelDeltaLaps", () => _plugin?.DeltaLaps },
                { "FuelCanPush", () => _plugin?.CanAffordToPush },
                { "FuelIsReady", () => _plugin?.IsFuelReady ?? false },
                { "PitStopsRequiredByFuel", () => _plugin?.PitStopsRequiredByFuel },
                { "PitWindowOpen", () => _plugin?.IsPitWindowOpen },

                // Flags and sessions (SimHub properties)
                { "FlagSessionFlags", () => _pluginManager?.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.SessionFlags") },
                { "PaceMode", () => _pluginManager?.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PaceMode") },
                { "SessionTypeName", () => _pluginManager?.GetPropertyValue("DataCorePlugin.GameData.SessionTypeName") },
                { "CompletedLaps", () => _pluginManager?.GetPropertyValue("DataCorePlugin.GameData.CompletedLaps") },
                { "PitServiceFuelDone", () => ReadPitServiceFuelDone() },

                // Traffic (legacy iRacingExtraProperties signal path removed)
                { "TrafficBehindGapSeconds", () => LegacyExtraSignalUnavailable("TrafficBehindGapSeconds") },
                { "TrafficBehindDistanceM", () => LegacyExtraSignalUnavailable("TrafficBehindDistanceM") },
                { "TrafficBehindClass", () => LegacyExtraSignalUnavailable("TrafficBehindClass") },
                { "PlayerClassName", () => LegacyExtraSignalUnavailable("PlayerClassName") },
                { "DriverAheadGapSeconds", () => LegacyExtraSignalUnavailable("DriverAheadGapSeconds") },
                { "FasterClassApproachLine", () => _plugin?.CurrentFasterClassApproachLine },

                // Pace / incident
                { "PlayerPaceLast5LapAvg", () => _plugin?.Pace_Last5LapAvgSec },
                { "PlayerClassPosition", () => _pluginManager?.GetPropertyValue("DataCorePlugin.GameData.PositionInClass") },
                { "IncidentCount", () => _pluginManager?.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PlayerCarDriverIncidentCount") },
                { "SlowDownTimeRemaining", () => LegacyExtraSignalUnavailable("SlowDownTimeRemaining") },
                { "IncidentAheadWarning", () => false }, // placeholder until implemented

                // Track markers (pit entry/exit assist)
                { "TrackMarkers.Pulse.Captured", () => _plugin?.ConsumeTrackMarkerCapturedPulse() },
                { "TrackMarkers.Pulse.LengthDelta", () => _plugin?.ConsumeTrackMarkerLengthDeltaPulse() },
                { "TrackMarkers.Pulse.LockedMismatch", () => _plugin?.ConsumeTrackMarkerLockedMismatchPulse() }
            };
        }

        private bool ReadPitServiceFuelDone()
        {
            try
            {
                var raw = _pluginManager?.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PlayerCarPitSvFlags");
                if (raw == null) return false;
                int flags = Convert.ToInt32(raw);
                // irsdk PitSvFlags.fuel_fill == 0x10
                return (flags & 0x10) != 0;
            }
            catch
            {
                return false;
            }
        }

        private object LegacyExtraSignalUnavailable(string signalId)
        {
            if (!_legacyExtraSignalWarned.Contains(signalId))
            {
                _legacyExtraSignalWarned.Add(signalId);
                SimHub.Logging.Current.Warn(
                    $"[LalaPlugin:MSGV1] Signal '{signalId}' has no native/plugin-owned authority. " +
                    "Legacy IRacingExtraProperties fallback is removed; signal remains unavailable.");
            }

            return null;
        }
    }
}
