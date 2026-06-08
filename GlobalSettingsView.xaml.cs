using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LaunchPlugin
{
    public partial class GlobalSettingsView : UserControl
    {
        private readonly ObservableCollection<IOverlayCategory> _iOverlayCategories = new ObservableCollection<IOverlayCategory>();

        public LalaLaunch Plugin { get; }
        public TelemetryTraceLogger TelemetryService { get; }

        public GlobalSettingsView(LalaLaunch plugin, TelemetryTraceLogger telemetry)
        {
            InitializeComponent();
            Plugin = plugin;
            TelemetryService = telemetry;
            DataContext = plugin;
            LaunchSettingsHost.Content = new LaunchPluginSettingsUI(plugin, telemetry);
            if (Plugin != null)
            {
                Plugin.DebugUiRefreshRequested += Plugin_DebugUiRefreshRequested;
            }
            Unloaded += GlobalSettingsView_Unloaded;
            InitializeIOverlayImport();
            SyncPropertySnapshotSelectAllFromGroups();
            RefreshPropertySnapshotRollingStatusText();
        }

        private void Plugin_DebugUiRefreshRequested()
        {
            Dispatcher.BeginInvoke(new Action(RefreshPropertySnapshotRollingStatusText));
        }

        private void GlobalSettingsView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (Plugin != null)
            {
                Plugin.DebugUiRefreshRequested -= Plugin_DebugUiRefreshRequested;
            }
        }

        private void PropertySnapshotSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin?.Settings == null || PropertySnapshotSelectAllCheckBox == null)
            {
                return;
            }

            bool allSelected = PropertySnapshotSelectAllCheckBox.IsChecked == true;
            Plugin.Settings.PropertySnapshotGroupFuelStrategy = allSelected;
            Plugin.Settings.PropertySnapshotGroupCarOppH2H = allSelected;
            Plugin.Settings.PropertySnapshotGroupPitPitExit = allSelected;
            Plugin.Settings.PropertySnapshotGroupShiftAssist = allSelected;
            Plugin.Settings.PropertySnapshotGroupMessageSystem = allSelected;
            Plugin.Settings.PropertySnapshotGroupLeagueClass = allSelected;
            Plugin.Settings.PropertySnapshotGroupRawDebug = allSelected;
            SyncPropertySnapshotGroupCheckboxes();
        }

        private void PropertySnapshotGroup_Click(object sender, RoutedEventArgs e)
        {
            SyncPropertySnapshotSelectAllFromGroups();
            RefreshPropertySnapshotRollingStatusText();
        }

        private void PropertySnapshotStartRolling_Click(object sender, RoutedEventArgs e)
        {
            Plugin?.StartPropertySnapshotRolling();
            RefreshPropertySnapshotRollingStatusText();
        }

        private void PropertySnapshotStopRolling_Click(object sender, RoutedEventArgs e)
        {
            Plugin?.StopPropertySnapshotRolling();
            RefreshPropertySnapshotRollingStatusText();
        }

        private void PropertySnapshotResetRollingCsv_Click(object sender, RoutedEventArgs e)
        {
            Plugin?.ResetPropertySnapshotRollingCsv();
            RefreshPropertySnapshotRollingStatusText();
        }

        private void DebugMasterToggle_Click(object sender, RoutedEventArgs e)
        {
            if (DebugMasterToggle?.IsChecked != true)
            {
                Plugin?.DisableDebugRuntimeSystemsForMasterGate();
            }

            RefreshPropertySnapshotRollingStatusText();
        }

        private void PropertySnapshotRollingStatusRefresh_Click(object sender, RoutedEventArgs e)
        {
            RefreshPropertySnapshotRollingStatusText();
        }

        private void PropertySnapshotRollingStatusRefresh_Click(object sender, SelectionChangedEventArgs e)
        {
            RefreshPropertySnapshotRollingStatusText();
        }

        private void PropertySnapshotRollingFrequencyTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            var binding = PropertySnapshotRollingFrequencyTextBox?.GetBindingExpression(TextBox.TextProperty);
            binding?.UpdateSource();
            Plugin?.NormalizePropertySnapshotRollingFrequencyForUi();
            binding?.UpdateTarget();
            RefreshPropertySnapshotRollingStatusText();
        }

        private void CarTrackingProbeStart_Click(object sender, RoutedEventArgs e)
        {
            Plugin?.StartCarTrackingProbeCsv();
        }

        private void CarTrackingProbeStop_Click(object sender, RoutedEventArgs e)
        {
            Plugin?.StopCarTrackingProbeCsv();
        }

        private void CarTrackingProbeResetCsv_Click(object sender, RoutedEventArgs e)
        {
            Plugin?.ResetCarTrackingProbeCsv();
        }

        private void RefreshPropertySnapshotRollingStatusText()
        {
            if (PropertySnapshotRollingStatusTextBlock == null)
            {
                return;
            }

            string status = (Plugin?.GetPropertySnapshotRollingStatusTextForUi() ?? "OFF").Trim().ToUpperInvariant();
            bool isReady = string.Equals(status, "READY", StringComparison.Ordinal);
            bool isRecording = string.Equals(status, "RECORDING", StringComparison.Ordinal);

            PropertySnapshotRollingStatusTextBlock.Text = status;

            if (PropertySnapshotRollingFrequencyTextBox != null && Plugin?.Settings != null)
            {
                Plugin.NormalizePropertySnapshotRollingFrequencyForUi();
                PropertySnapshotRollingFrequencyTextBox.GetBindingExpression(TextBox.TextProperty)?.UpdateTarget();
            }

            if (PropertySnapshotStartRollingButton != null)
            {
                PropertySnapshotStartRollingButton.IsEnabled = isReady;
            }

            if (PropertySnapshotStopRollingButton != null)
            {
                PropertySnapshotStopRollingButton.IsEnabled = isRecording;
            }

            if (PropertySnapshotResetRollingCsvButton != null)
            {
                PropertySnapshotResetRollingCsvButton.IsEnabled = isReady || isRecording;
            }

            if (PropertySnapshotRollingStatusBorder == null)
            {
                return;
            }

            switch (status)
            {
                case "RECORDING":
                    PropertySnapshotRollingStatusBorder.Background = CreateBrush("#13351F");
                    PropertySnapshotRollingStatusBorder.BorderBrush = CreateBrush("#2CFF7A");
                    PropertySnapshotRollingStatusTextBlock.Foreground = CreateBrush("#7CFFA7");
                    break;
                case "READY":
                    PropertySnapshotRollingStatusBorder.Background = CreateBrush("#11263A");
                    PropertySnapshotRollingStatusBorder.BorderBrush = CreateBrush("#4FB7FF");
                    PropertySnapshotRollingStatusTextBlock.Foreground = CreateBrush("#78CCFF");
                    break;
                default:
                    PropertySnapshotRollingStatusBorder.Background = CreateBrush("#222222");
                    PropertySnapshotRollingStatusBorder.BorderBrush = CreateBrush("#666666");
                    PropertySnapshotRollingStatusTextBlock.Foreground = CreateBrush("#D8D8D8");
                    break;
            }
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            return (SolidColorBrush)new BrushConverter().ConvertFromString(hex);
        }

        private void SyncPropertySnapshotSelectAllFromGroups()
        {
            if (Plugin?.Settings == null)
            {
                return;
            }

            bool allSelected = Plugin.Settings.PropertySnapshotGroupFuelStrategy
                && Plugin.Settings.PropertySnapshotGroupCarOppH2H
                && Plugin.Settings.PropertySnapshotGroupPitPitExit
                && Plugin.Settings.PropertySnapshotGroupShiftAssist
                && Plugin.Settings.PropertySnapshotGroupMessageSystem
                && Plugin.Settings.PropertySnapshotGroupLeagueClass
                && Plugin.Settings.PropertySnapshotGroupRawDebug;

            Plugin.Settings.PropertySnapshotSelectAllGroups = allSelected;
            if (PropertySnapshotSelectAllCheckBox != null)
            {
                PropertySnapshotSelectAllCheckBox.IsChecked = allSelected;
            }
        }

        private void SyncPropertySnapshotGroupCheckboxes()
        {
            if (Plugin?.Settings == null)
            {
                return;
            }

            if (PropertySnapshotFuelStrategyCheckBox != null) PropertySnapshotFuelStrategyCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupFuelStrategy;
            if (PropertySnapshotCarOppH2HCheckBox != null) PropertySnapshotCarOppH2HCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupCarOppH2H;
            if (PropertySnapshotPitPitExitCheckBox != null) PropertySnapshotPitPitExitCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupPitPitExit;
            if (PropertySnapshotShiftAssistCheckBox != null) PropertySnapshotShiftAssistCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupShiftAssist;
            if (PropertySnapshotMessageSystemCheckBox != null) PropertySnapshotMessageSystemCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupMessageSystem;
            if (PropertySnapshotLeagueClassCheckBox != null) PropertySnapshotLeagueClassCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupLeagueClass;
            if (PropertySnapshotRawDebugCheckBox != null) PropertySnapshotRawDebugCheckBox.IsChecked = Plugin.Settings.PropertySnapshotGroupRawDebug;
            SyncPropertySnapshotSelectAllFromGroups();
        }

        private void AddFriend_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin?.Settings == null)
            {
                return;
            }

            if (Plugin.Settings.Friends == null)
            {
                Plugin.Settings.Friends = new ObservableCollection<LaunchPluginFriendEntry>();
            }

            Plugin.Settings.Friends.Add(new LaunchPluginFriendEntry { Name = "Friend", UserId = 0, Tag = LaunchPluginFriendEntry.TagFriend });
            Plugin.NotifyFriendsChanged();
        }

        private void RemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin?.Settings?.Friends == null)
            {
                return;
            }

            var entry = (sender as FrameworkElement)?.DataContext as LaunchPluginFriendEntry;
            if (entry == null)
            {
                return;
            }

            Plugin.Settings.Friends.Remove(entry);
            Plugin.NotifyFriendsChanged();
        }

        private void FriendsGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            Plugin?.NotifyFriendsChanged();
        }

        private void InitializeIOverlayImport()
        {
            if (IOverlayCategoryComboBox == null)
            {
                return;
            }

            IOverlayCategoryComboBox.ItemsSource = _iOverlayCategories;
            IOverlayCategoryComboBox.DisplayMemberPath = nameof(IOverlayCategory.Name);
            LoadIOverlayCategories(showMessageOnError: false);
        }

        private void ReloadIOverlay_Click(object sender, RoutedEventArgs e)
        {
            LoadIOverlayCategories(showMessageOnError: true);
        }

        private void ImportIOverlay_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin?.Settings == null)
            {
                return;
            }

            var selectedCategory = IOverlayCategoryComboBox?.SelectedItem as IOverlayCategory;
            if (selectedCategory == null || string.IsNullOrWhiteSpace(selectedCategory.Id))
            {
                MessageBox.Show("Select a tag category first.", "Import iOverlay", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!TryReadIOverlaySettings(out var categories, out var driverTags, out var errorMessage))
            {
                MessageBox.Show(errorMessage, "Import iOverlay", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            UpdateIOverlayCategories(categories);

            var selectedCategoryId = selectedCategory.Id;
            var importTag = IOverlayTagComboBox?.SelectedItem as string;
            var normalizedImportTag = LaunchPluginFriendEntry.NormalizeTag(importTag);

            if (Plugin.Settings.Friends == null)
            {
                Plugin.Settings.Friends = new ObservableCollection<LaunchPluginFriendEntry>();
            }

            var existingById = new Dictionary<int, LaunchPluginFriendEntry>();
            foreach (var entry in Plugin.Settings.Friends)
            {
                if (entry == null)
                {
                    continue;
                }

                var id = entry.UserId;
                if (id <= 0)
                {
                    continue;
                }

                if (!existingById.ContainsKey(id))
                {
                    existingById[id] = entry;
                }
            }

            int newCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var driverTag in driverTags)
            {
                if (!string.Equals(driverTag.TagId, selectedCategoryId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!int.TryParse(driverTag.Identifier, out var userId) || userId <= 0)
                {
                    skippedCount++;
                    continue;
                }

                if (existingById.TryGetValue(userId, out var existing))
                {
                    bool updated = false;
                    if (!string.Equals(existing.Tag, normalizedImportTag, StringComparison.OrdinalIgnoreCase))
                    {
                        existing.Tag = normalizedImportTag;
                        updated = true;
                    }

                    if (IsDefaultFriendName(existing.Name) && !string.IsNullOrWhiteSpace(driverTag.Name))
                    {
                        existing.Name = driverTag.Name.Trim();
                        updated = true;
                    }

                    if (updated)
                    {
                        updatedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }

                    continue;
                }

                var newEntry = new LaunchPluginFriendEntry
                {
                    Name = string.IsNullOrWhiteSpace(driverTag.Name) ? "Friend" : driverTag.Name.Trim(),
                    UserId = userId,
                    Tag = normalizedImportTag
                };

                Plugin.Settings.Friends.Add(newEntry);
                existingById[userId] = newEntry;
                newCount++;
            }

            Plugin.NotifyFriendsChanged();
            MessageBox.Show($"Imported {newCount} new ({updatedCount} updated), {skippedCount} skipped", "Import iOverlay", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void LoadIOverlayCategories(bool showMessageOnError)
        {
            if (!TryReadIOverlaySettings(out var categories, out _, out var errorMessage))
            {
                UpdateIOverlayCategories(new List<IOverlayCategory>());
                IOverlayStatusText.Text = errorMessage;
                if (showMessageOnError)
                {
                    MessageBox.Show(errorMessage, "Import iOverlay", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                return;
            }

            UpdateIOverlayCategories(categories);
            IOverlayStatusText.Text = categories.Count == 0 ? "No driver tag categories found in iOverlay settings.dat." : string.Empty;
        }

        private void UpdateIOverlayCategories(IEnumerable<IOverlayCategory> categories)
        {
            _iOverlayCategories.Clear();
            foreach (var category in categories)
            {
                _iOverlayCategories.Add(category);
            }
        }

        private bool TryReadIOverlaySettings(out List<IOverlayCategory> categories, out List<IOverlayDriverTag> driverTags, out string errorMessage)
        {
            categories = new List<IOverlayCategory>();
            driverTags = new List<IOverlayDriverTag>();
            errorMessage = null;

            string settingsPath = GetIOverlaySettingsPath();
            if (!File.Exists(settingsPath))
            {
                errorMessage = $"iOverlay settings.dat not found at:{Environment.NewLine}{settingsPath}";
                return false;
            }

            try
            {
                var json = File.ReadAllText(settingsPath);
                var root = JObject.Parse(json);

                var categoriesToken = root.SelectToken("modules.drivertagging.tagcategory") as JArray;
                if (categoriesToken != null)
                {
                    foreach (var token in categoriesToken)
                    {
                        if (token == null)
                        {
                            continue;
                        }

                        var idToken = token["id"];
                        if (idToken == null)
                        {
                            continue;
                        }

                        var id = idToken.ToString().Trim();
                        if (string.IsNullOrWhiteSpace(id))
                        {
                            continue;
                        }

                        var name = token["name"]?.ToString();
                        categories.Add(new IOverlayCategory(id, string.IsNullOrWhiteSpace(name) ? id : name.Trim()));
                    }
                }

                var tagsToken = root.SelectToken("modules.drivertagging.drivertag") as JArray;
                if (tagsToken != null)
                {
                    foreach (var token in tagsToken)
                    {
                        if (token == null)
                        {
                            continue;
                        }

                        var identifier = token["identifier"]?.ToString();
                        var name = token["name"]?.ToString();
                        var tagId = token["tagId"]?.ToString();

                        if (string.IsNullOrWhiteSpace(identifier) || string.IsNullOrWhiteSpace(tagId))
                        {
                            continue;
                        }

                        driverTags.Add(new IOverlayDriverTag(identifier.Trim(), name?.Trim(), tagId.Trim()));
                    }
                }
            }
            catch (JsonException)
            {
                errorMessage = "Failed to parse iOverlay settings.dat (JSON).";
                return false;
            }
            catch (Exception ex)
            {
                errorMessage = $"Failed to read iOverlay settings.dat: {ex.Message}";
                return false;
            }

            return true;
        }

        private static bool IsDefaultFriendName(string name)
        {
            return string.IsNullOrWhiteSpace(name) || string.Equals(name.Trim(), "Friend", StringComparison.OrdinalIgnoreCase);
        }

        private static string GetIOverlaySettingsPath()
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "iOverlay", "settings.dat");
        }

        private sealed class IOverlayCategory
        {
            public IOverlayCategory(string id, string name)
            {
                Id = id;
                Name = name;
            }

            public string Id { get; }

            public string Name { get; }
        }

        private sealed class IOverlayDriverTag
        {
            public IOverlayDriverTag(string identifier, string name, string tagId)
            {
                Identifier = identifier;
                Name = name;
                TagId = tagId;
            }

            public string Identifier { get; }

            public string Name { get; }

            public string TagId { get; }
        }

        private void BrowseLeagueClassCsv_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin?.Settings == null)
            {
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*";
            dialog.CheckFileExists = true;
            dialog.Multiselect = false;
            if (dialog.ShowDialog() == true)
            {
                Plugin.Settings.LeagueClassCsvPath = dialog.FileName;
                Plugin.ReloadLeagueClassConfig();
            }
        }

        private void ReloadLeagueClassCsv_Click(object sender, RoutedEventArgs e)
        {
            if (Plugin?.Settings == null || !Plugin.Settings.LeagueClassEnabled)
            {
                return;
            }

            Plugin.ReloadLeagueClassConfig();
        }

    }
}
