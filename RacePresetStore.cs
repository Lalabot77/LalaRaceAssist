// File: RacePresetStore.cs
// Purpose: Single-file JSON persistence for RacePreset[].
// Target: C# 7.3 / .NET Framework (Newtonsoft.Json preferred)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace LaunchPlugin
{
    public static class RacePresetStore
    {
        [JsonObject(MemberSerialization.OptIn)]
        private class RacePresetStoreRoot
        {
            [JsonProperty]
            public int SchemaVersion { get; set; } = 1;

            [JsonProperty]
            public List<RacePreset> Presets { get; set; } = new List<RacePreset>();
        }

        private const string NewFileName = "RacePresets.json";
        private const string LegacyFileName = "LalaLaunch.RacePresets.json";
        public static string GetFolderPath()
        {
            return PluginStorage.GetPluginFolder();
        }

        public static string GetFilePath() => Path.Combine(GetFolderPath(), NewFileName);

        // --- keep DefaultPresets(), SaveAll(), etc. unchanged ---

        public static List<RacePreset> LoadAll()
        {
            try
            {
                // 1) Ensure PluginsData\Common exists
                var folder = GetFolderPath();
                var path = GetFilePath();
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

                // 2) One-time migration from legacy locations if present
                MigrateFromLegacyIfPresent(path);

                if (!File.Exists(path)) { var d = DefaultPresets(); SaveAll(d); return d; }

                var json = File.ReadAllText(path);
                List<RacePreset> list = null;
                try
                {
                    var store = JsonConvert.DeserializeObject<RacePresetStoreRoot>(json);
                    list = store?.Presets;
                }
                catch
                {
                    list = null;
                }

                if (list == null)
                {
                    list = JsonConvert.DeserializeObject<List<RacePreset>>(json);
                }

                if (list == null) list = new List<RacePreset>();

                if (list.Count == 0) { var d = DefaultPresets(); SaveAll(d); return d; }
                return list;
            }
            catch (Exception ex)
            {
                TryBackupCorrupt();
                var d = DefaultPresets(); SafeTry(() => SaveAll(d));
                DebugWrite("RacePresetStore: Error loading presets, wrote defaults. " + ex.Message);
                return d;
            }
        }

        // One-time migration helper
        private static void MigrateFromLegacyIfPresent(string destPath)
        {
            try
            {
                if (File.Exists(destPath))
                    return;

                var legacyCommonPath = PluginStorage.GetCommonFilePath(LegacyFileName);
                if (PluginStorage.TryMigrate(legacyCommonPath, destPath))
                    return;

                var docs = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
                var oldFolder = Path.Combine(docs, "SimHub", "LalaLaunch");
                var oldPath = Path.Combine(oldFolder, LegacyFileName);
                PluginStorage.TryMigrate(oldPath, destPath);
            }
            catch { /* best-effort */ }
        }

        /// <summary>
        /// Save all presets atomically (write to temp then replace).
        /// </summary>
        public static void SaveAll(List<RacePreset> presets)
        {
            if (presets == null) throw new ArgumentNullException(nameof(presets));

            var folder = GetFolderPath();
            var path = GetFilePath();
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var store = new RacePresetStoreRoot
            {
                Presets = presets
            };
            var json = JsonConvert.SerializeObject(store, Formatting.Indented);

            var temp = path + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(path))
                File.Replace(temp, path, path + ".bak", ignoreMetadataErrors: true);
            else
                File.Move(temp, path);

            DebugWrite($"RacePresetStore: Saved {presets.Count} preset(s).");
        }

        /// <summary>
        /// Embedded shipped defaults for first-run preset seeding.
        /// </summary>
        public static List<RacePreset> DefaultPresets()
        {
            return new List<RacePreset>
            {
                new RacePreset
                {
                    Name = "IMSA 40m",
                    Type = RacePresetType.TimeLimited,
                    RaceMinutes = 40,
                    RaceLaps = null,
                    PreRaceMode = 1,
                    TireChangeTimeSec = 0.0,
                    MaxFuelPercent = 49.8,
                    ContingencyInLaps = true,
                    ContingencyValue = 1.9047486670090663
                },
                new RacePreset
                {
                    Name = "Sprint 20m",
                    Type = RacePresetType.TimeLimited,
                    RaceMinutes = 20,
                    RaceLaps = null,
                    PreRaceMode = 3,
                    TireChangeTimeSec = 0.0,
                    MaxFuelPercent = null,
                    LegacyMaxFuelLitres = 55.079360442729552,
                    ContingencyInLaps = true,
                    ContingencyValue = 1.5
                },
                new RacePreset
                {
                    Name = "Fixed 30 Laps",
                    Type = RacePresetType.LapLimited,
                    RaceMinutes = null,
                    RaceLaps = 30,
                    PreRaceMode = 3,
                    TireChangeTimeSec = 0.0,
                    MaxFuelPercent = null,
                    LegacyMaxFuelLitres = 110.0,
                    ContingencyInLaps = true,
                    ContingencyValue = 1.0
                },
                new RacePreset
                {
                    Name = "Thursday SRi League",
                    Type = RacePresetType.TimeLimited,
                    RaceMinutes = 60,
                    RaceLaps = null,
                    PreRaceMode = 1,
                    TireChangeTimeSec = 0.0,
                    MaxFuelPercent = 70.0,
                    ContingencyInLaps = true,
                    ContingencyValue = 1.4331941999189148
                },
                new RacePreset
                {
                    Name = "24 Hours Endurance",
                    Type = RacePresetType.TimeLimited,
                    RaceMinutes = 1440,
                    RaceLaps = null,
                    PreRaceMode = 3,
                    TireChangeTimeSec = 23.231592413663829,
                    MaxFuelPercent = null,
                    LegacyMaxFuelLitres = 119.89939586875516,
                    ContingencyInLaps = true,
                    ContingencyValue = 1.0
                },
                new RacePreset
                {
                    Name = "6 Hour GT3 Endurance",
                    Type = RacePresetType.TimeLimited,
                    RaceMinutes = 360,
                    RaceLaps = null,
                    PreRaceMode = 2,
                    TireChangeTimeSec = 23.0,
                    MaxFuelPercent = null,
                    LegacyMaxFuelLitres = 110.0,
                    ContingencyInLaps = true,
                    ContingencyValue = 1.0
                }
            };
        }

        private static void TryBackupCorrupt()
        {
            try
            {
                var path = GetFilePath();
                if (File.Exists(path))
                {
                    var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var bak = Path.Combine(GetFolderPath(), $"RacePresets.corrupt.{stamp}.json");
                    File.Copy(path, bak, overwrite: false);
                    DebugWrite("RacePresetStore: Backed up possible corrupt file -> " + bak);
                }
            }
            catch { /* ignore backup issues */ }
        }

        private static void DebugWrite(string msg)
        {
            // No SimHub dependency here. If you have a logger, call it from outside.
            Debug.WriteLine(msg);
        }

        private static void SafeTry(Action a)
        {
            try { a(); } catch { /* swallow */ }
        }
    }
}
