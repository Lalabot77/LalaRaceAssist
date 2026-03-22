using System;
using System.Windows;
using System.Windows.Controls;
namespace LaunchPlugin
{
    /// <summary>
    /// Interaction logic for FuelCalculatorView.xaml
    /// </summary>
    public partial class FuelCalculatorView : UserControl
    {
        // Store a reference to the FuelCalcs engine
        private readonly FuelCalcs _fuelCalcs;
        private bool _isPresetManagerOpen;

        public FuelCalculatorView(FuelCalcs fuelCalcs)
        {
            InitializeComponent();
            _fuelCalcs = fuelCalcs;
            this.DataContext = _fuelCalcs;
        }

        private void OnOpenPresetManager(object sender, RoutedEventArgs e)
        {
            if (_isPresetManagerOpen)
            {
                return;
            }

            _isPresetManagerOpen = true;

            try
            {
                var presetManager = new PresetsManagerView(_fuelCalcs);
                var owner = Window.GetWindow(this);

                var dialog = new Window
                {
                    Title = "Preset Manager",
                    Content = presetManager,
                    Width = 980,
                    Height = 760,
                    MinWidth = 860,
                    MinHeight = 620,
                    ResizeMode = ResizeMode.CanResize,
                    Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x23, 0x2B)),
                    WindowStartupLocation = owner != null && owner.IsVisible ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                    ShowInTaskbar = false
                };

                if (owner != null && owner.IsVisible)
                {
                    dialog.Owner = owner;
                }

                presetManager.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x23, 0x2B));

                dialog.ShowDialog();
            }
            catch (Exception ex)
            {
                SimHub.Logging.Current.Error("[LalaPlugin:Fuel Burn] Failed to open Preset Manager: " + ex.Message);
                MessageBox.Show(
                    "Unable to open the Preset Manager. See SimHub logs for details.",
                    "Preset Manager",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            finally
            {
                _isPresetManagerOpen = false;
            }
        }
    }
}
