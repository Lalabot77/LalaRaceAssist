// In file: LaunchPluginCombinedSettingsControl.xaml.cs
using SimHub.Plugins.Styles;
using System.Windows.Controls;

namespace LaunchPlugin
{
    public partial class LaunchPluginCombinedSettingsControl : UserControl
    {
        public LaunchPluginCombinedSettingsControl(LalaLaunch mainPluginInstance, TelemetryTraceLogger telemetryTraceLoggerService)
        {
            InitializeComponent();

            var overviewTab = new SHTabItem
            {
                Header = "OVERVIEW",
                Content = new OverviewTabView(mainPluginInstance)
            };
            MainTabControl.Items.Add(overviewTab);

            var strategyTab = new SHTabItem
            {
                Header = "STRATEGY",
                Content = new FuelCalculatorView(mainPluginInstance.FuelCalculator)
            };
            MainTabControl.Items.Add(strategyTab);

            var profilesTab = new SHTabItem
            {
                Header = "PROFILES",
                Content = new ProfilesManagerView(mainPluginInstance.ProfilesViewModel)
            };
            MainTabControl.Items.Add(profilesTab);

            var dashesTab = new SHTabItem
            {
                Header = "DASH CONTROL",
                Content = new DashesTabView(mainPluginInstance)
            };
            MainTabControl.Items.Add(dashesTab);

            var launchAnalysisTab = new SHTabItem
            {
                Header = "LAUNCH ANALYSIS",
                Content = new LaunchAnalysisControl(telemetryTraceLoggerService)
            };
            MainTabControl.Items.Add(launchAnalysisTab);

            var settingsTab = new SHTabItem
            {
                Header = "SETTINGS",
                Content = new GlobalSettingsView(mainPluginInstance, telemetryTraceLoggerService)
            };
            MainTabControl.Items.Add(settingsTab);
        }
    }
}
