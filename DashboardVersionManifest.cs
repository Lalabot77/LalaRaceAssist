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
        private static readonly string[] RequiredAssetKeys = new[]
        {
            "Lala-Driver Dash",
            "Lala-Strategy Dash",
            "Lala-Alerts Overlay",
            "Lala-VerticalTrafficBar Overlay",
            "Lala-Head2Head",
            "Lala-Fuel Calculator"
        };

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
            string manifestJson = ReadEmbeddedManifestJson();
            if (string.IsNullOrWhiteSpace(manifestJson))
            {
                result.StatusText = "Embedded version manifest missing";
                return result;
            }

            try
            {
                return Parse(manifestJson, "embedded");
            }
            catch (Exception ex)
            {
                result.StatusText = "Embedded version manifest invalid: " + ex.Message;
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

        private static DashboardVersionManifest Parse(string json, string sourceLabel)
        {
            var result = new DashboardVersionManifest();
            var root = JObject.Parse(json);
            string rootFamily = (root.Value<string>("releaseFamily") ?? string.Empty).Trim();
            result.CompatiblePluginFamily = rootFamily;
            if (string.IsNullOrWhiteSpace(rootFamily))
            {
                throw new InvalidDataException("releaseFamily missing");
            }

            var assetsObject = root["assets"] as JObject;
            if (assetsObject == null)
            {
                throw new InvalidDataException("assets missing");
            }

            var assets = new List<DashboardVersionInfo>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string requiredKey in RequiredAssetKeys)
            {
                var asset = assetsObject[requiredKey] as JObject;
                if (asset == null)
                {
                    throw new InvalidDataException("required asset missing: " + requiredKey);
                }

                string displayName = (asset.Value<string>("displayName") ?? string.Empty).Trim();
                string latest = (asset.Value<string>("latest") ?? string.Empty).Trim();
                string family = (asset.Value<string>("compatiblePluginFamily") ?? rootFamily).Trim();
                if (string.IsNullOrWhiteSpace(displayName))
                {
                    throw new InvalidDataException(requiredKey + " displayName missing");
                }
                if (string.IsNullOrWhiteSpace(latest))
                {
                    throw new InvalidDataException(requiredKey + " latest version missing");
                }
                if (string.IsNullOrWhiteSpace(family))
                {
                    throw new InvalidDataException(requiredKey + " compatible plugin family missing");
                }
                bool releaseCritical;
                if (!TryReadBoolean(asset["releaseCritical"], out releaseCritical))
                {
                    throw new InvalidDataException(requiredKey + " releaseCritical missing or invalid");
                }

                assets.Add(new DashboardVersionInfo
                {
                    Key = requiredKey,
                    PropertyKey = ToPropertyKey(requiredKey),
                    DisplayName = displayName,
                    Latest = latest,
                    CompatiblePluginFamily = family,
                    ReleaseCritical = releaseCritical
                });
                seen.Add(requiredKey);
            }

            foreach (var property in assetsObject.Properties())
            {
                if (!seen.Contains(property.Name))
                {
                    throw new InvalidDataException("unrecognised asset key: " + property.Name);
                }
            }

            result.Assets = assets;
            result.Valid = true;
            result.StatusText = "Version manifest loaded from " + sourceLabel + " resource";
            return result;
        }

        private static string ReadEmbeddedManifestJson()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = null;
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (name.EndsWith(ManifestFileName, StringComparison.Ordinal))
                {
                    resourceName = name;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(resourceName))
            {
                return null;
            }

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        private static bool TryReadBoolean(JToken token, out bool value)
        {
            value = false;
            if (token == null) return false;
            if (token.Type == JTokenType.Boolean)
            {
                value = token.Value<bool>();
                return true;
            }
            if (token.Type == JTokenType.String)
            {
                return bool.TryParse(token.Value<string>(), out value);
            }
            return false;
        }

        private static string ToPropertyKey(string manifestKey)
        {
            switch (manifestKey)
            {
                case "Lala-Driver Dash": return "DriverDash";
                case "Lala-Strategy Dash": return "StrategyDash";
                case "Lala-Alerts Overlay": return "AlertsOverlay";
                case "Lala-VerticalTrafficBar Overlay": return "VerticalTrafficBar";
                case "Lala-Head2Head": return "Head2Head";
                case "Lala-Fuel Calculator": return "FuelCalculator";
                default: return string.Empty;
            }
        }
    }
}
