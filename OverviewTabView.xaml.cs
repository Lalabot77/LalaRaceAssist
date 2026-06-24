using Newtonsoft.Json.Linq;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace LaunchPlugin
{
    public partial class OverviewTabView : UserControl, INotifyPropertyChanged
    {
        private const string RepositoryUrl = "https://github.com/Lalabot77/LalaRaceAssist";
        private const string ReleasesUrl = "https://github.com/Lalabot77/LalaRaceAssist/releases";
        private const string IssuesUrl = "https://github.com/Lalabot77/LalaRaceAssist/issues";
        private const string QuickStartUrl = "https://github.com/Lalabot77/LalaRaceAssist/blob/main/Docs/Quick_Start.md";
        private const string UserGuideUrl = "https://github.com/Lalabot77/LalaRaceAssist/blob/main/Docs/User_Guide.md";
        private const string LatestReleaseApiUrl = "https://api.github.com/repos/Lalabot77/LalaRaceAssist/releases/latest";

        private readonly LalaLaunch _plugin;
        private readonly DispatcherTimer _statusTimer;
        private static readonly HttpClient ReleaseClient = CreateReleaseClient();
        private DateTime _lastCheckUtc = DateTime.MinValue;

        private string _installedVersionText;
        private string _latestVersionText = "Unknown";
        private string _updateStateText = "Unable to check";
        private string _linkStatusText = string.Empty;
        private string _currentGameText = "Not detected";
        private string _pluginLoadedText = "Loaded";
        private string _profilesStatusText = "Unknown";
        private string _trackMarkersStatusText = "Unknown";
        private string _currentCarText = "Not detected";
        private string _currentTrackText = "Not detected";

        private string _monitorSystemText = "Unknown";
        private string _monitorSystemBackground = "#404040";
        private string _monitorSystemForeground = "#FFFFFF";
        private string _leagueClassStatusText = "OFF";
        private string _leagueClassDetailText = "League Class disabled";
        private string _leagueClassBackground = "#404040";
        private string _leagueClassForeground = "#FFFFFF";

        public event PropertyChangedEventHandler PropertyChanged;

        public string InstalledVersionText { get => _installedVersionText; private set { _installedVersionText = value; OnPropertyChanged(nameof(InstalledVersionText)); } }
        public string LatestVersionText { get => _latestVersionText; private set { _latestVersionText = value; OnPropertyChanged(nameof(LatestVersionText)); } }
        public string UpdateStateText { get => _updateStateText; private set { _updateStateText = value; OnPropertyChanged(nameof(UpdateStateText)); } }
        public string LinkStatusText { get => _linkStatusText; private set { _linkStatusText = value; OnPropertyChanged(nameof(LinkStatusText)); } }
        public string CurrentGameText { get => _currentGameText; private set { _currentGameText = value; OnPropertyChanged(nameof(CurrentGameText)); } }
        public string PluginLoadedText { get => _pluginLoadedText; private set { _pluginLoadedText = value; OnPropertyChanged(nameof(PluginLoadedText)); } }
        public string ProfilesStatusText { get => _profilesStatusText; private set { _profilesStatusText = value; OnPropertyChanged(nameof(ProfilesStatusText)); } }
        public string TrackMarkersStatusText { get => _trackMarkersStatusText; private set { _trackMarkersStatusText = value; OnPropertyChanged(nameof(TrackMarkersStatusText)); } }
        public string CurrentCarText { get => _currentCarText; private set { _currentCarText = value; OnPropertyChanged(nameof(CurrentCarText)); } }
        public string CurrentTrackText { get => _currentTrackText; private set { _currentTrackText = value; OnPropertyChanged(nameof(CurrentTrackText)); } }

        public string MonitorSystemText { get => _monitorSystemText; private set { _monitorSystemText = value; OnPropertyChanged(nameof(MonitorSystemText)); } }
        public string MonitorSystemBackground { get => _monitorSystemBackground; private set { _monitorSystemBackground = value; OnPropertyChanged(nameof(MonitorSystemBackground)); } }
        public string MonitorSystemForeground { get => _monitorSystemForeground; private set { _monitorSystemForeground = value; OnPropertyChanged(nameof(MonitorSystemForeground)); } }
        public string LeagueClassStatusText { get => _leagueClassStatusText; private set { _leagueClassStatusText = value; OnPropertyChanged(nameof(LeagueClassStatusText)); } }
        public string LeagueClassDetailText { get => _leagueClassDetailText; private set { _leagueClassDetailText = value; OnPropertyChanged(nameof(LeagueClassDetailText)); } }
        public string LeagueClassBackground { get => _leagueClassBackground; private set { _leagueClassBackground = value; OnPropertyChanged(nameof(LeagueClassBackground)); } }
        public string LeagueClassForeground { get => _leagueClassForeground; private set { _leagueClassForeground = value; OnPropertyChanged(nameof(LeagueClassForeground)); } }

        public OverviewTabView(LalaLaunch plugin)
        {
            InitializeComponent();
            _plugin = plugin;
            InstalledVersionText = GetInstalledVersionText();

            DataContext = this;
            RefreshStatusSnapshot();

            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _statusTimer.Tick += (s, e) => RefreshStatusSnapshot();
            _statusTimer.Start();

            _ = CheckLatestReleaseAsync(force: false);
        }

        private static HttpClient CreateReleaseClient()
        {
            var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("LalaRaceAssist-OverviewTab/1.1");
            return client;
        }

        private static string GetInstalledVersionText()
        {
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null)
            {
                return "Unknown";
            }

            if (version.Build == 0 && version.Revision == 0)
            {
                return $"v{version.Major}.{version.Minor}";
            }

            if (version.Revision == 0)
            {
                return $"v{version.Major}.{version.Minor}.{version.Build}";
            }

            return $"v{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        private void ResetPlugin_Click(object sender, RoutedEventArgs e)
        {
            _plugin?.TriggerManualRecoveryReset("Manual reset button");
        }

        private void RefreshStatusSnapshot()
        {
            if (_plugin == null)
            {
                PluginLoadedText = "Not loaded";
                ProfilesStatusText = "Unavailable";
                TrackMarkersStatusText = "Unavailable";
                CurrentCarText = "Unavailable";
                CurrentTrackText = "Unavailable";
                CurrentGameText = "Unavailable";
                MonitorSystemText = "Unavailable";
                LeagueClassStatusText = "Unavailable";
                LeagueClassDetailText = "Plugin not loaded";
                return;
            }

            PluginLoadedText = "Loaded";

            var profileCount = _plugin.ProfilesViewModel?.CarProfiles?.Count ?? 0;
            ProfilesStatusText = profileCount > 0 ? profileCount + " profile(s)" : "No profiles found";

            var trackKey = _plugin.CurrentTrackKey;
            if (!string.IsNullOrWhiteSpace(trackKey))
            {
                var marker = _plugin.GetTrackMarkersSnapshot(trackKey);
                TrackMarkersStatusText = marker.HasData ? "Available" : "No markers yet";
            }
            else
            {
                TrackMarkersStatusText = "No track loaded";
            }

            CurrentCarText = string.IsNullOrWhiteSpace(_plugin.CurrentCarModel) ? "Not detected" : _plugin.CurrentCarModel;
            CurrentTrackText = string.IsNullOrWhiteSpace(_plugin.CurrentTrackName) ? "Not detected" : _plugin.CurrentTrackName;

            var currentGame = _plugin.PluginManager?.GetPropertyValue("DataCorePlugin.CurrentGame");
            CurrentGameText = currentGame == null ? "Not detected" : currentGame.ToString();

            RefreshMonitorSystemSnapshot();
            RefreshLeagueClassSnapshot();
        }

        private void RefreshMonitorSystemSnapshot()
        {
            MonitorSystemText = ReadPluginProperty("MonitorSystem.Text", "MONITOR READY");
            MonitorSystemBackground = ReadPluginProperty("MonitorSystem.BackgroundColour", "#0B5D1E");
            MonitorSystemForeground = ReadPluginProperty("MonitorSystem.TextColour", "#FFFFFF");
        }

        private void RefreshLeagueClassSnapshot()
        {
            if (_plugin?.Settings?.LeagueClassEnabled != true)
            {
                LeagueClassStatusText = "OFF";
                LeagueClassDetailText = "League Class disabled";
                LeagueClassBackground = "#404040";
                LeagueClassForeground = "#FFFFFF";
                return;
            }

            var status = _plugin.LeagueClassStatus;
            int loaded = status?.LoadedCount ?? 0;
            int valid = status?.ValidDriverCount ?? 0;
            int invalid = status?.InvalidRowCount ?? 0;
            int duplicates = status?.DuplicateRowCount ?? 0;
            string config = status?.ConfigStatusText ?? "Status unavailable";
            string player = _plugin.LeagueClassPlayerPreviewText ?? string.Empty;
            bool csvBackedMode = _plugin.LeagueClassShowCsvSection;
            bool playerWaiting = player.IndexOf("not available yet", StringComparison.OrdinalIgnoreCase) >= 0;
            bool invalidManualOverride = player.IndexOf("manual override invalid", StringComparison.OrdinalIgnoreCase) >= 0;
            bool playerResolved = !invalidManualOverride &&
                player.IndexOf("Source: NONE", StringComparison.OrdinalIgnoreCase) < 0 &&
                player.IndexOf("unresolved", StringComparison.OrdinalIgnoreCase) < 0;
            bool configBad = config.IndexOf("fail", StringComparison.OrdinalIgnoreCase) >= 0 ||
                config.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                config.IndexOf("invalid", StringComparison.OrdinalIgnoreCase) >= 0 ||
                config.IndexOf("error", StringComparison.OrdinalIgnoreCase) >= 0;
            bool rowCountWarning = csvBackedMode && (loaded <= 0 || valid <= 0);
            bool warning = rowCountWarning || configBad || invalid > 0 || duplicates > 0 || invalidManualOverride || (!playerResolved && !playerWaiting);

            LeagueClassStatusText = warning ? "WARNING" : "ACTIVE";
            LeagueClassBackground = warning ? "#D97A00" : "#0B5D1E";
            LeagueClassForeground = "#FFFFFF";

            string playerDetail = playerWaiting ? "Player: waiting for session" :
                (invalidManualOverride ? "Player manual override invalid" : (playerResolved ? "Player resolved" : "Player unresolved"));
            LeagueClassDetailText = string.Format("{0} | Loaded {1}, valid {2}, invalid {3}, duplicates {4} | {5}",
                config, loaded, valid, invalid, duplicates, playerDetail);
        }

        private string ReadPluginProperty(string propertyName, string fallback)
        {
            var value = _plugin?.PluginManager?.GetPropertyValue("LalaLaunch." + propertyName) ??
                _plugin?.PluginManager?.GetPropertyValue(propertyName);
            return value == null ? fallback : (value.ToString() ?? fallback);
        }

        private async Task CheckLatestReleaseAsync(bool force)
        {
            if (!force && (DateTime.UtcNow - _lastCheckUtc) < TimeSpan.FromMinutes(10))
            {
                return;
            }

            try
            {
                UpdateStateText = "Checking...";
                var response = await ReleaseClient.GetAsync(LatestReleaseApiUrl);
                if (!response.IsSuccessStatusCode)
                {
                    UpdateStateText = "Unable to check";
                    return;
                }

                var payload = await response.Content.ReadAsStringAsync();
                var latestTag = ExtractLatestTag(payload);
                if (string.IsNullOrWhiteSpace(latestTag))
                {
                    UpdateStateText = "Unable to check";
                    return;
                }

                LatestVersionText = latestTag;
                UpdateStateText = CompareVersionStrings(InstalledVersionText, latestTag);
                _lastCheckUtc = DateTime.UtcNow;
            }
            catch
            {
                UpdateStateText = "Unable to check";
            }
        }

        private static string ExtractLatestTag(string json)
        {
            var obj = JObject.Parse(json);
            var tag = obj.Value<string>("tag_name");
            if (!string.IsNullOrWhiteSpace(tag))
            {
                return tag.Trim();
            }

            var name = obj.Value<string>("name");
            return string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        }

        private static string CompareVersionStrings(string installedText, string latestText)
        {
            if (!TryParseVersion(installedText, out var installedVersion) || !TryParseVersion(latestText, out var latestVersion))
            {
                return "Unable to check";
            }

            return installedVersion >= latestVersion ? "Up to date" : "Update available";
        }

        private static bool TryParseVersion(string value, out Version version)
        {
            var cleaned = (value ?? string.Empty).Trim();
            if (cleaned.StartsWith("v", StringComparison.OrdinalIgnoreCase))
            {
                cleaned = cleaned.Substring(1);
            }

            return Version.TryParse(cleaned, out version);
        }

        private void OpenUrlSafe(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                LinkStatusText = string.Empty;
            }
            catch
            {
                LinkStatusText = "Unable to open link on this system.";
            }
        }

        private async void CheckNow_Click(object sender, RoutedEventArgs e)
        {
            await CheckLatestReleaseAsync(force: true);
        }

        private void OpenReleases_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlSafe(ReleasesUrl);
        }

        private void QuickStart_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlSafe(QuickStartUrl);
        }

        private void UserGuide_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlSafe(UserGuideUrl);
        }

        private void Repository_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlSafe(RepositoryUrl);
        }

        private void Releases_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlSafe(ReleasesUrl);
        }

        private void Issues_Click(object sender, RoutedEventArgs e)
        {
            OpenUrlSafe(IssuesUrl);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
