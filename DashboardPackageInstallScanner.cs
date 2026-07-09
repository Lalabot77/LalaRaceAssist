using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace LaunchPlugin
{
    public enum DashboardPackageInstallStatus
    {
        Unknown = 0,
        NotInstalled = 1,
        Installed = 2
    }

    public sealed class DashboardPackageInstallInfo
    {
        public string PropertyKey { get; set; }
        public DashboardPackageInstallStatus Status { get; set; }
        public bool Found { get; set; }
        public string InstalledVersion { get; set; }
        public bool Parseable { get; set; }
        public string FullPath { get; set; }
        public string FailureReason { get; set; }

        public string StatusText
        {
            get
            {
                switch (Status)
                {
                    case DashboardPackageInstallStatus.Installed: return "INSTALLED";
                    case DashboardPackageInstallStatus.NotInstalled: return "NOT INSTALLED";
                    default: return "UNKNOWN";
                }
            }
        }
    }

    public sealed class DashboardPackageInstallScanResult
    {
        public bool SimHubRootFound { get; set; }
        public string SimHubRootPath { get; set; }
        public string DashTemplatesPath { get; set; }
        public string StatusText { get; set; }
        public IReadOnlyDictionary<string, DashboardPackageInstallInfo> Packages { get; set; }

        public DashboardPackageInstallInfo FindByPropertyKey(string propertyKey)
        {
            if (Packages == null || string.IsNullOrWhiteSpace(propertyKey)) return null;
            DashboardPackageInstallInfo info;
            return Packages.TryGetValue(propertyKey, out info) ? info : null;
        }
    }

    public sealed class DashboardPackageInstallScanner
    {
        private const string DashTemplatesFolderName = "DashTemplates";
        private const string DashFilesPrefix = "Dash Files/";

        public DashboardPackageInstallScanResult Scan(DashboardVersionManifest manifest)
        {
            var packages = new Dictionary<string, DashboardPackageInstallInfo>(StringComparer.Ordinal);
            string simHubRoot;
            string dashTemplates;
            string rootFailure;
            bool rootFound = TryResolveSimHubRoot(out simHubRoot, out dashTemplates, out rootFailure);

            if (manifest != null)
            {
                foreach (var asset in manifest.Assets)
                {
                    if (asset == null || string.IsNullOrWhiteSpace(asset.PropertyKey)) continue;
                    packages[asset.PropertyKey] = rootFound
                        ? ScanAsset(asset, dashTemplates)
                        : Unknown(asset.PropertyKey, string.Empty, rootFailure);
                }
            }

            return new DashboardPackageInstallScanResult
            {
                SimHubRootFound = rootFound,
                SimHubRootPath = simHubRoot ?? string.Empty,
                DashTemplatesPath = dashTemplates ?? string.Empty,
                StatusText = rootFound ? "Installed dashboard scan complete" : rootFailure,
                Packages = packages
            };
        }

        private static DashboardPackageInstallInfo ScanAsset(DashboardVersionInfo asset, string dashTemplatesPath)
        {
            string expectedPath;
            string pathFailure;
            if (!TryBuildExpectedPath(asset, dashTemplatesPath, out expectedPath, out pathFailure))
            {
                return Unknown(asset.PropertyKey, string.Empty, pathFailure);
            }

            if (!File.Exists(expectedPath))
            {
                return new DashboardPackageInstallInfo
                {
                    PropertyKey = asset.PropertyKey,
                    Status = DashboardPackageInstallStatus.NotInstalled,
                    Found = false,
                    InstalledVersion = "Not installed",
                    Parseable = false,
                    FullPath = expectedPath,
                    FailureReason = "Expected dashboard file not found"
                };
            }

            try
            {
                var root = JObject.Parse(File.ReadAllText(expectedPath));
                var metadata = root["Metadata"] as JObject;
                string versionProperty = string.IsNullOrWhiteSpace(asset.VersionProperty) ? "DashboardVersion" : asset.VersionProperty.Trim();
                string installedVersion = metadata == null ? string.Empty : ((metadata.Value<string>(versionProperty) ?? string.Empty).Trim());
                if (string.IsNullOrWhiteSpace(installedVersion))
                {
                    return Unknown(asset.PropertyKey, expectedPath, versionProperty + " metadata missing");
                }

                if (!DashboardVersionManifest.IsParseableVersion(installedVersion))
                {
                    return Unknown(asset.PropertyKey, expectedPath, versionProperty + " metadata unparsable");
                }

                return new DashboardPackageInstallInfo
                {
                    PropertyKey = asset.PropertyKey,
                    Status = DashboardPackageInstallStatus.Installed,
                    Found = true,
                    InstalledVersion = installedVersion,
                    Parseable = true,
                    FullPath = expectedPath,
                    FailureReason = string.Empty
                };
            }
            catch (Exception ex)
            {
                return Unknown(asset.PropertyKey, expectedPath, "Dashboard JSON unreadable: " + ex.Message);
            }
        }

        private static DashboardPackageInstallInfo Unknown(string propertyKey, string fullPath, string reason)
        {
            return new DashboardPackageInstallInfo
            {
                PropertyKey = propertyKey,
                Status = DashboardPackageInstallStatus.Unknown,
                Found = false,
                InstalledVersion = "Unknown",
                Parseable = false,
                FullPath = fullPath ?? string.Empty,
                FailureReason = string.IsNullOrWhiteSpace(reason) ? "Installed dashboard version unknown" : reason
            };
        }

        private static bool TryResolveSimHubRoot(out string simHubRoot, out string dashTemplates, out string failureReason)
        {
            simHubRoot = string.Empty;
            dashTemplates = string.Empty;
            failureReason = "SimHub DashTemplates folder not found";

            string assemblyLocation = Assembly.GetExecutingAssembly().Location;
            if (string.IsNullOrWhiteSpace(assemblyLocation))
            {
                failureReason = "Plugin DLL location unavailable";
                return false;
            }

            var dllDirectory = Path.GetDirectoryName(assemblyLocation);
            if (string.IsNullOrWhiteSpace(dllDirectory))
            {
                failureReason = "Plugin DLL directory unavailable";
                return false;
            }

            return TryResolveSimHubRootFromDllDirectory(dllDirectory, out simHubRoot, out dashTemplates, out failureReason);
        }

        internal static bool TryResolveSimHubRootFromDllDirectory(string dllDirectory, out string simHubRoot, out string dashTemplates, out string failureReason)
        {
            simHubRoot = string.Empty;
            dashTemplates = string.Empty;
            failureReason = "DashTemplates folder not found beside plugin DLL directory or parent";

            if (string.IsNullOrWhiteSpace(dllDirectory))
            {
                failureReason = "Plugin DLL directory unavailable";
                return false;
            }

            var pluginDirectory = new DirectoryInfo(dllDirectory);
            string primaryDashTemplates = Path.Combine(pluginDirectory.FullName, DashTemplatesFolderName);
            if (Directory.Exists(primaryDashTemplates))
            {
                simHubRoot = pluginDirectory.FullName;
                dashTemplates = primaryDashTemplates;
                failureReason = string.Empty;
                return true;
            }

            var parentDirectory = pluginDirectory.Parent;
            if (parentDirectory != null)
            {
                string fallbackDashTemplates = Path.Combine(parentDirectory.FullName, DashTemplatesFolderName);
                if (Directory.Exists(fallbackDashTemplates))
                {
                    simHubRoot = parentDirectory.FullName;
                    dashTemplates = fallbackDashTemplates;
                    failureReason = string.Empty;
                    return true;
                }
            }

            simHubRoot = pluginDirectory.FullName;
            dashTemplates = primaryDashTemplates;
            return false;
        }

        private static bool TryBuildExpectedPath(DashboardVersionInfo asset, string dashTemplatesPath, out string expectedPath, out string failureReason)
        {
            expectedPath = string.Empty;
            failureReason = string.Empty;
            if (asset == null)
            {
                failureReason = "Manifest asset missing";
                return false;
            }

            string installFolder = ResolveInstallFolder(asset.Folder);
            string fileName = (asset.File ?? string.Empty).Trim();
            if (!IsSafeRelativeSegment(installFolder) || !IsSafeRelativeSegment(fileName))
            {
                failureReason = "Manifest install folder or file is not safe";
                return false;
            }

            string candidate = Path.GetFullPath(Path.Combine(dashTemplatesPath, installFolder, fileName));
            string dashTemplatesFullPath = Path.GetFullPath(dashTemplatesPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar);
            if (!candidate.StartsWith(dashTemplatesFullPath, StringComparison.OrdinalIgnoreCase))
            {
                failureReason = "Manifest install path escapes DashTemplates";
                return false;
            }

            expectedPath = candidate;
            return true;
        }

        private static string ResolveInstallFolder(string manifestFolder)
        {
            string folder = (manifestFolder ?? string.Empty).Trim().Replace('\\', '/');
            if (folder.StartsWith(DashFilesPrefix, StringComparison.OrdinalIgnoreCase))
            {
                folder = folder.Substring(DashFilesPrefix.Length);
            }

            folder = folder.Trim('/');
            int lastSlash = folder.LastIndexOf('/');
            return lastSlash >= 0 ? folder.Substring(lastSlash + 1) : folder;
        }

        private static bool IsSafeRelativeSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            if (Path.IsPathRooted(value)) return false;
            if (value.IndexOf("..", StringComparison.Ordinal) >= 0) return false;
            if (value.IndexOfAny(Path.GetInvalidPathChars()) >= 0) return false;
            return true;
        }
    }
}
