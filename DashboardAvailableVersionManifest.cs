using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LaunchPlugin
{
    public sealed class DashboardAvailableVersionInfo
    {
        public string Key { get; private set; }
        public string PropertyKey { get; private set; }
        public string Latest { get; private set; }
        public string MinimumSupported { get; private set; }
        public string CompatiblePluginFamily { get; private set; }
        public string RequiresPlugin { get; private set; }

        internal DashboardAvailableVersionInfo(string key, string propertyKey, string latest, string minimumSupported, string compatiblePluginFamily, string requiresPlugin)
        {
            Key = key;
            PropertyKey = propertyKey;
            Latest = latest;
            MinimumSupported = minimumSupported;
            CompatiblePluginFamily = compatiblePluginFamily;
            RequiresPlugin = requiresPlugin;
        }
    }

    public sealed class DashboardAvailableVersionCheckResult
    {
        public bool Valid { get; private set; }
        public string StatusText { get; private set; }
        public IReadOnlyDictionary<string, DashboardAvailableVersionInfo> Packages { get; private set; }

        internal DashboardAvailableVersionCheckResult(bool valid, string statusText, IReadOnlyDictionary<string, DashboardAvailableVersionInfo> packages)
        {
            Valid = valid;
            StatusText = statusText;
            Packages = packages;
        }

        public DashboardAvailableVersionInfo FindByPropertyKey(string propertyKey)
        {
            DashboardAvailableVersionInfo info;
            return Packages != null && Packages.TryGetValue(propertyKey ?? string.Empty, out info) ? info : null;
        }
    }

    public sealed class DashboardAvailableVersionManifest
    {
        // Keep remote data limited to version/compatibility metadata. Never use it for paths or file names.
        public const string RawManifestUrl = "https://raw.githubusercontent.com/Lalabot77/LalaRaceAssist/main/LalaRaceAssist.VersionManifest.json";
        private static readonly Regex RequiresPluginPattern = new Regex(@"^\s*>=\s*(v?\d+(?:\.\d+){1,3})\s+<\s*(v?\d+(?:\.\d+){1,3})\s*$", RegexOptions.Compiled);

        public async Task<DashboardAvailableVersionCheckResult> FetchAsync(DashboardVersionManifest embeddedManifest)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("LalaRaceAssist-OverviewTab/1.1");
                    var response = await client.GetAsync(RawManifestUrl).ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                    {
                        return Unavailable("GitHub dashboard manifest request failed");
                    }
                    var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return Parse(json, embeddedManifest);
                }
            }
            catch
            {
                return Unavailable("GitHub dashboard manifest unavailable");
            }
        }

        public static DashboardAvailableVersionCheckResult Parse(string json, DashboardVersionManifest embeddedManifest)
        {
            try
            {
                if (embeddedManifest == null || !embeddedManifest.Valid) throw new InvalidDataException("Embedded package catalogue unavailable");
                var root = JObject.Parse(json ?? string.Empty);
                string family = (ReadManifestString(root, "releaseFamily", false) ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(family)) throw new InvalidDataException("releaseFamily missing");
                if (!(root["plugin"] is JObject)) throw new InvalidDataException("plugin section missing");
                var assets = root["assets"] as JObject;
                if (assets == null || assets.Count != embeddedManifest.Assets.Count) throw new InvalidDataException("assets catalogue incomplete");

                var packages = new Dictionary<string, DashboardAvailableVersionInfo>(StringComparer.Ordinal);
                foreach (var embedded in embeddedManifest.Assets)
                {
                    var asset = assets[embedded.Key] as JObject;
                    if (asset == null) throw new InvalidDataException("required asset missing: " + embedded.Key);
                    string latest = (ReadManifestString(asset, "latest", false) ?? string.Empty).Trim();
                    string min = (ReadManifestString(asset, "minimumSupported", true) ?? string.Empty).Trim();
                    string assetFamily = (ReadManifestString(asset, "compatiblePluginFamily", true) ?? family).Trim();
                    string requires = (ReadManifestString(asset, "requiresPlugin", false) ?? string.Empty).Trim();
                    if (!DashboardVersionManifest.IsParseableVersion(latest)) throw new InvalidDataException(embedded.Key + " latest version invalid");
                    if (min.Length > 0 && !DashboardVersionManifest.IsParseableVersion(min)) throw new InvalidDataException(embedded.Key + " minimumSupported invalid");
                    if (assetFamily.Length == 0 || requires.Length == 0 || !RequiresPluginPattern.IsMatch(requires)) throw new InvalidDataException(embedded.Key + " plugin compatibility invalid");
                    packages.Add(embedded.PropertyKey, new DashboardAvailableVersionInfo(embedded.Key, embedded.PropertyKey, latest, min, assetFamily, requires));
                }
                var knownKeys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var embedded in embeddedManifest.Assets) knownKeys.Add(embedded.Key);
                foreach (var property in assets.Properties())
                {
                    if (!knownKeys.Contains(property.Name)) throw new InvalidDataException("unexpected asset key");
                }
                return new DashboardAvailableVersionCheckResult(true, "GitHub dashboard manifest loaded", new ReadOnlyDictionary<string, DashboardAvailableVersionInfo>(packages));
            }
            catch (Exception ex)
            {
                return Unavailable("GitHub dashboard manifest invalid: " + ex.Message);
            }
        }

        public static bool TryCompareVersions(string left, string right, out int comparison)
        {
            comparison = 0;
            if (!DashboardVersionManifest.IsParseableVersion(left) || !DashboardVersionManifest.IsParseableVersion(right)) return false;
            string a = left.Trim().TrimStart('v', 'V');
            string b = right.Trim().TrimStart('v', 'V');
            string[] aa = a.Split('.');
            string[] bb = b.Split('.');
            for (int i = 0; i < 4; i++)
            {
                string av = i < aa.Length ? aa[i].TrimStart('0') : "0";
                string bv = i < bb.Length ? bb[i].TrimStart('0') : "0";
                if (av.Length == 0) av = "0";
                if (bv.Length == 0) bv = "0";
                if (av.Length != bv.Length) { comparison = av.Length.CompareTo(bv.Length); return true; }
                int partComparison = string.CompareOrdinal(av, bv);
                if (partComparison != 0) { comparison = partComparison; return true; }
            }
            return true;
        }

        public static bool PluginSatisfies(string requirement, string pluginVersion)
        {
            var match = RequiresPluginPattern.Match(requirement ?? string.Empty);
            int lower, upper;
            return match.Success && TryCompareVersions(pluginVersion, match.Groups[1].Value, out lower) && lower >= 0 &&
                TryCompareVersions(pluginVersion, match.Groups[2].Value, out upper) && upper < 0;
        }

        public static string ResolvePackageStatus(DashboardPackageInstallInfo installed, DashboardAvailableVersionInfo available, string bundledPluginFamily, string installedPluginVersion)
        {
            if (installed == null || installed.Status == DashboardPackageInstallStatus.Unknown) return "UNKNOWN";
            if (installed.Status == DashboardPackageInstallStatus.NotInstalled) return "NOT INSTALLED";
            if (available == null) return "CHECK UNAVAILABLE";
            int comparison;
            if (!TryCompareVersions(installed.InstalledVersion, available.Latest, out comparison)) return "UNKNOWN";
            if (comparison == 0) return "UP TO DATE";
            if (comparison > 0) return "NEWER THAN AVAILABLE";
            bool incompatibleFamily = !string.Equals(available.CompatiblePluginFamily, bundledPluginFamily, StringComparison.OrdinalIgnoreCase);
            bool pluginTooOld = !PluginSatisfies(available.RequiresPlugin, installedPluginVersion);
            return incompatibleFamily || pluginTooOld ? "UPDATE REQUIRED" : "UPDATE AVAILABLE";
        }

        private static DashboardAvailableVersionCheckResult Unavailable(string text)
        {
            return new DashboardAvailableVersionCheckResult(false, text, new ReadOnlyDictionary<string, DashboardAvailableVersionInfo>(new Dictionary<string, DashboardAvailableVersionInfo>(StringComparer.Ordinal)));
        }

        private static string ReadManifestString(JObject obj, string propertyName, bool optional)
        {
            var token = obj[propertyName];
            if (token == null && optional) return null;
            if (token == null || token.Type != JTokenType.String) throw new InvalidDataException(propertyName + " missing or not a string");
            return token.Value<string>();
        }
    }
}
