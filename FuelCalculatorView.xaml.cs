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
        public FuelCalculatorView(FuelCalcs fuelCalcs)
        {
            InitializeComponent();
            _fuelCalcs = fuelCalcs;
            this.DataContext = _fuelCalcs;
        }

        private void OnOpenPresetManager(object sender, RoutedEventArgs e)
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
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                Owner = owner,
                ShowInTaskbar = false
            };

            dialog.ShowDialog();
        }
    }
}
