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
            return version == null ? "Unknown" : version.ToString();
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
