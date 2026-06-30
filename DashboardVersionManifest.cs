using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LaunchPlugin
{
    public sealed class DashboardVersionInfo
    {
        public string Key { get; set; }
        public string PropertyKey { get; set; }
        public string DisplayName { get; set; }
        public string Latest { get; set; }
        public string CompatiblePluginFamily { get; set; }
        public bool ReleaseCritical { get; set; }
    }

    public sealed class DashboardVersionManifest
    {
        private const string ManifestFileName = "LalaRaceAssist.VersionManifest.json";

        public bool Valid { get; private set; }
        public string StatusText { get; private set; }
        public string CompatiblePluginFamily { get; private set; }
        public IReadOnlyList<DashboardVersionInfo> Assets { get; private set; }

        private DashboardVersionManifest()
        {
            Valid = false;
            StatusText = "Manifest not loaded";
            CompatiblePluginFamily = string.Empty;
            Assets = new List<DashboardVersionInfo>();
        }

        public static DashboardVersionManifest Load()
        {
            var result = new DashboardVersionManifest();
            string path = ResolveManifestPath();
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            {
                result.StatusText = "Version manifest missing";
                return result;
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(path));
                result.CompatiblePluginFamily = (root.Value<string>("releaseFamily") ?? string.Empty).Trim();
                var assetsObject = root["assets"] as JObject;
                if (assetsObject == null)
                {
                    result.StatusText = "Version manifest invalid: assets missing";
                    return result;
                }

                var assets = new List<DashboardVersionInfo>();
                foreach (var property in assetsObject.Properties())
                {
                    var asset = property.Value as JObject;
                    if (asset == null) continue;

                    string displayName = (asset.Value<string>("displayName") ?? property.Name).Trim();
                    string latest = (asset.Value<string>("latest") ?? string.Empty).Trim();
                    string family = (asset.Value<string>("compatiblePluginFamily") ?? result.CompatiblePluginFamily).Trim();
                    if (string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(latest))
                    {
                        result.StatusText = "Version manifest invalid: asset version missing";
                        return result;
                    }

                    assets.Add(new DashboardVersionInfo
                    {
                        Key = property.Name,
                        PropertyKey = ToPropertyKey(property.Name),
                        DisplayName = displayName,
                        Latest = latest,
                        CompatiblePluginFamily = family,
                        ReleaseCritical = asset.Value<bool?>("releaseCritical") ?? false
                    });
                }

                result.Assets = assets;
                result.Valid = true;
                result.StatusText = "Version manifest loaded";
                return result;
            }
            catch
            {
                result.StatusText = "Version manifest invalid";
                return result;
            }
        }

        public DashboardVersionInfo FindByPropertyKey(string propertyKey)
        {
            foreach (var asset in Assets)
            {
                if (string.Equals(asset.PropertyKey, propertyKey, StringComparison.Ordinal)) return asset;
            }
            return null;
        }

        private static string ResolveManifestPath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var candidates = new List<string>();
            if (!string.IsNullOrWhiteSpace(baseDir))
            {
                candidates.Add(Path.Combine(baseDir, ManifestFileName));
                candidates.Add(Path.Combine(baseDir, "..", ManifestFileName));
                candidates.Add(Path.Combine(baseDir, "..", "..", ManifestFileName));
            }

            string assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrWhiteSpace(assemblyDir))
            {
                candidates.Add(Path.Combine(assemblyDir, ManifestFileName));
                candidates.Add(Path.Combine(assemblyDir, "..", ManifestFileName));
                candidates.Add(Path.Combine(assemblyDir, "..", "..", ManifestFileName));
            }

            candidates.Add(Path.Combine(Environment.CurrentDirectory, ManifestFileName));

            foreach (var candidate in candidates)
            {
                try
                {
                    string full = Path.GetFullPath(candidate);
                    if (File.Exists(full)) return full;
                }
                catch { }
            }

            return null;
        }

        private static string ToPropertyKey(string manifestKey)
        {
            string key = manifestKey ?? string.Empty;
            key = key.Replace("Lala-", string.Empty).Replace(" ", string.Empty).Replace("-", string.Empty);
            if (string.Equals(key, "DriverDash", StringComparison.OrdinalIgnoreCase)) return "DriverDash";
            if (string.Equals(key, "StrategyDash", StringComparison.OrdinalIgnoreCase)) return "StrategyDash";
            if (string.Equals(key, "AlertsOverlay", StringComparison.OrdinalIgnoreCase)) return "AlertsOverlay";
            if (string.Equals(key, "VerticalTrafficBarOverlay", StringComparison.OrdinalIgnoreCase)) return "VerticalTrafficBar";
            if (string.Equals(key, "Head2Head", StringComparison.OrdinalIgnoreCase)) return "Head2Head";
            if (string.Equals(key, "FuelCalculator", StringComparison.OrdinalIgnoreCase)) return "FuelCalculator";
            return key;
        }
    }
}
