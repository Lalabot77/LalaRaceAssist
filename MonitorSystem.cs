using System;

namespace LaunchPlugin
{
    internal enum MonitorSeverity
    {
        Off = 0,
        Ok = 1,
        Watch = 2,
        Caution = 3,
        Warning = 4,
        Fault = 5,
        Recovered = 6
    }

    internal sealed class MonitorSystem
    {
        private const string OffBackgroundColour = "#202020";
        private const string OffTextColour = "#C0C0C0";
        private const string OkBackgroundColour = "#0B5D1E";
        private const string WatchBackgroundColour = "#F0A000";
        private const string WarningBackgroundColour = "#FF0000";
        private const string RecoveredBackgroundColour = "#007C91";
        private const string LightTextColour = "#FFFFFF";
        private const string DarkTextColour = "#000000";
        private const string WarningTextColour = "#FFFF00";

        public string StateText { get; private set; }
        public string DisplayText { get; private set; }
        public string BackgroundColour { get; private set; }
        public string TextColour { get; private set; }
        public int Enum { get; private set; }
        public bool IsEnabled { get; private set; }
        public bool IsFuelDataCheckActive => IsEnabled && string.Equals(DisplayText, "FUEL DATA CHECK", StringComparison.Ordinal);
        public bool IsFuelHealthAlertActive =>
            IsEnabled &&
            (string.Equals(DisplayText, "FUEL DATA CHECK", StringComparison.Ordinal) ||
             string.Equals(DisplayText, "FUEL DATA FAULT", StringComparison.Ordinal));
        public bool IsPhase2BPitWarningActive =>
            IsEnabled &&
            (string.Equals(DisplayText, "REFUEL OFF", StringComparison.Ordinal) ||
             string.Equals(DisplayText, "MFD FUEL LOW", StringComparison.Ordinal) ||
             string.Equals(DisplayText, "EXIT FUEL SHORT", StringComparison.Ordinal));
        public bool IsBaselineFuelWarningActive =>
            IsEnabled &&
            string.Equals(DisplayText, "BASELINE SHORT", StringComparison.Ordinal);
        public bool IsMonitorPitWarningActive => IsPhase2BPitWarningActive || IsBaselineFuelWarningActive;

        public MonitorSystem()
        {
            SetEnabled(true);
        }

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (enabled)
            {
                SetOutput("ON", MonitorSeverity.Ok, "MONITOR READY", OkBackgroundColour, LightTextColour);
            }
            else
            {
                SetOutput("OFF", MonitorSeverity.Off, "MONITOR OFF", OffBackgroundColour, OffTextColour);
            }
        }

        public void Publish(MonitorSeverity severity, string text)
        {
            if (!IsEnabled)
            {
                return;
            }

            string displayText = string.IsNullOrWhiteSpace(text) ? "MONITOR READY" : text.Trim();
            string backgroundColour;
            string textColour = LightTextColour;

            switch (severity)
            {
                case MonitorSeverity.Off:
                    SetEnabled(false);
                    return;
                case MonitorSeverity.Watch:
                case MonitorSeverity.Caution:
                    backgroundColour = WatchBackgroundColour;
                    textColour = DarkTextColour;
                    break;
                case MonitorSeverity.Warning:
                case MonitorSeverity.Fault:
                    backgroundColour = WarningBackgroundColour;
                    textColour = WarningTextColour;
                    break;
                case MonitorSeverity.Recovered:
                    backgroundColour = RecoveredBackgroundColour;
                    break;
                default:
                    backgroundColour = OkBackgroundColour;
                    break;
            }

            SetOutput("ON", severity, displayText, backgroundColour, textColour);
        }

        private void SetOutput(
            string stateText,
            MonitorSeverity severity,
            string displayText,
            string backgroundColour,
            string textColour)
        {
            int enumValue = (int)severity;
            if (string.Equals(StateText, stateText, StringComparison.Ordinal) &&
                string.Equals(DisplayText, displayText, StringComparison.Ordinal) &&
                string.Equals(BackgroundColour, backgroundColour, StringComparison.Ordinal) &&
                string.Equals(TextColour, textColour, StringComparison.Ordinal) &&
                Enum == enumValue)
            {
                return;
            }

            StateText = stateText;
            DisplayText = displayText;
            BackgroundColour = backgroundColour;
            TextColour = textColour;
            Enum = enumValue;
        }
    }
}
