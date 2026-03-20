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
                    WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen,
                    Owner = owner,
                    ShowInTaskbar = false
                };

                presetManager.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x1F, 0x23, 0x2B));

                dialog.ShowDialog();
            }
            finally
            {
                _isPresetManagerOpen = false;
            }
        }
    }
}
