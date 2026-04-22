using System;

namespace LaunchPlugin
{
    internal enum PitTyreControlMode
    {
        Off = 0,
        Dry = 1,
        Wet = 2,
        Auto = 3
    }

    internal sealed class PitTyreControlSnapshot
    {
        public bool IsTireServiceSelected;
        public bool HasRequestedCompound;
        public int RequestedCompound;
        public bool HasPlayerCompound;
        public int PlayerCompound;
        public bool WeatherDeclaredWet;
        public string AvailableCompound01 = string.Empty;
        public string AvailableCompound02 = string.Empty;
    }

    internal sealed class PitTyreControlEngine
    {
        private const int SendCooldownMs = 900;
        private const int MaxAttemptsPerTarget = 2;

        private readonly Func<PitTyreControlSnapshot> _snapshotProvider;
        private readonly Func<string, string, string, bool> _rawCommandSender;
        private readonly Action<PitCommandAction> _builtInActionSender;
        private readonly Action<string, string, string> _feedbackPublisher;
        private readonly Action<string> _logger;

        private string _lastServiceTargetKey = string.Empty;
        private int _serviceTargetAttempts;
        private DateTime _serviceNextAttemptUtc = DateTime.MinValue;

        private string _lastCompoundTargetKey = string.Empty;
        private int _compoundTargetAttempts;
        private DateTime _compoundNextAttemptUtc = DateTime.MinValue;

        public PitTyreControlMode Mode { get; private set; } = PitTyreControlMode.Off;

        public PitTyreControlEngine(
            Func<PitTyreControlSnapshot> snapshotProvider,
            Func<string, string, string, bool> rawCommandSender,
            Action<PitCommandAction> builtInActionSender,
            Action<string, string, string> feedbackPublisher,
            Action<string> logger)
        {
            _snapshotProvider = snapshotProvider;
            _rawCommandSender = rawCommandSender;
            _builtInActionSender = builtInActionSender;
            _feedbackPublisher = feedbackPublisher;
            _logger = logger;
        }

        public string ModeText => ModeToText(Mode);

        public void ResetToOff()
        {
            Mode = PitTyreControlMode.Off;
            ResetAttemptState();
        }

        public void ModeCycle()
        {
            switch (Mode)
            {
                case PitTyreControlMode.Off:
                    SetMode(PitTyreControlMode.Dry, "Pit.TyreControl.ModeCycle");
                    break;
                case PitTyreControlMode.Dry:
                    SetMode(PitTyreControlMode.Wet, "Pit.TyreControl.ModeCycle");
                    break;
                case PitTyreControlMode.Wet:
                    SetMode(PitTyreControlMode.Auto, "Pit.TyreControl.ModeCycle");
                    break;
                default:
                    SetMode(PitTyreControlMode.Off, "Pit.TyreControl.ModeCycle");
                    break;
            }
        }

        public void SetOff() => SetMode(PitTyreControlMode.Off, "Pit.TyreControl.SetOff");
        public void SetDry() => SetMode(PitTyreControlMode.Dry, "Pit.TyreControl.SetDry");
        public void SetWet() => SetMode(PitTyreControlMode.Wet, "Pit.TyreControl.SetWet");
        public void SetAuto() => SetMode(PitTyreControlMode.Auto, "Pit.TyreControl.SetAuto");

        public void OnTelemetryTick()
        {
            var snapshot = _snapshotProvider();
            if (snapshot == null)
            {
                return;
            }

            bool desiredService = Mode != PitTyreControlMode.Off;
            EnsureTyreService(snapshot, desiredService);

            if (Mode == PitTyreControlMode.Off)
            {
                ResetCompoundAttempts();
                return;
            }

            bool desiredWet = Mode == PitTyreControlMode.Wet ||
                              (Mode == PitTyreControlMode.Auto && snapshot.WeatherDeclaredWet);

            EnsureCompound(snapshot, desiredWet);
        }

        private void SetMode(PitTyreControlMode mode, string actionName)
        {
            Mode = mode;
            ResetAttemptState();
            PublishSelectionFeedback(actionName, string.Format("TYRE CHANGE {0}", ModeToText(mode)));
        }

        private void EnsureTyreService(PitTyreControlSnapshot snapshot, bool desiredSelected)
        {
            string targetKey = desiredSelected ? "ON" : "OFF";
            if (snapshot.IsTireServiceSelected == desiredSelected)
            {
                _lastServiceTargetKey = targetKey;
                _serviceTargetAttempts = 0;
                _serviceNextAttemptUtc = DateTime.MinValue;
                return;
            }

            if (!string.Equals(_lastServiceTargetKey, targetKey, StringComparison.Ordinal))
            {
                _lastServiceTargetKey = targetKey;
                _serviceTargetAttempts = 0;
                _serviceNextAttemptUtc = DateTime.MinValue;
            }

            if (!CanAttempt(_serviceTargetAttempts, _serviceNextAttemptUtc))
            {
                return;
            }

            _builtInActionSender?.Invoke(PitCommandAction.ToggleTyresAll);
            _serviceTargetAttempts++;
            _serviceNextAttemptUtc = DateTime.UtcNow.AddMilliseconds(SendCooldownMs);
        }

        private void EnsureCompound(PitTyreControlSnapshot snapshot, bool desiredWet)
        {
            string targetKey = desiredWet ? "wet" : "dry";
            bool alreadyCorrect = snapshot.HasRequestedCompound &&
                                  IsRequestedCompoundInDesiredFamily(snapshot.RequestedCompound, desiredWet);
            if (alreadyCorrect)
            {
                _lastCompoundTargetKey = targetKey;
                _compoundTargetAttempts = 0;
                _compoundNextAttemptUtc = DateTime.MinValue;
                return;
            }

            if (!string.Equals(_lastCompoundTargetKey, targetKey, StringComparison.Ordinal))
            {
                _lastCompoundTargetKey = targetKey;
                _compoundTargetAttempts = 0;
                _compoundNextAttemptUtc = DateTime.MinValue;
            }

            if (!CanAttempt(_compoundTargetAttempts, _compoundNextAttemptUtc))
            {
                return;
            }

            string command = desiredWet ? "#tc 2$" : "#tc 0$";
            string actionName = desiredWet ? "Pit.TyreControl.SetWetCompound" : "Pit.TyreControl.SetDryCompound";
            string feedback = desiredWet ? "TYRE CHANGE WET" : "TYRE CHANGE DRY";
            LogCompoundAttempt(snapshot, desiredWet, command);
            _rawCommandSender?.Invoke(actionName, command, feedback);
            _compoundTargetAttempts++;
            _compoundNextAttemptUtc = DateTime.UtcNow.AddMilliseconds(SendCooldownMs);
        }

        private void LogCompoundAttempt(PitTyreControlSnapshot snapshot, bool desiredWet, string command)
        {
            string requested = snapshot.HasRequestedCompound ? snapshot.RequestedCompound.ToString() : "NA";
            string player = snapshot.HasPlayerCompound ? snapshot.PlayerCompound.ToString() : "NA";
            string wetFlag = snapshot.WeatherDeclaredWet ? "true" : "false";
            string desired = desiredWet ? "WET" : "DRY";
            _logger?.Invoke(
                $"[LalaPlugin:PitTyreControl] Compound change attempt target={desired} cmd='{command}' requested={requested} player={player} weatherDeclaredWet={wetFlag} available01='{snapshot.AvailableCompound01 ?? string.Empty}' available02='{snapshot.AvailableCompound02 ?? string.Empty}'");
        }

        private void PublishSelectionFeedback(string actionName, string message)
        {
            _feedbackPublisher?.Invoke(actionName, message, string.Empty);
        }

        private static bool IsRequestedCompoundInDesiredFamily(int requestedCompound, bool desiredWet)
        {
            if (desiredWet)
            {
                return requestedCompound == 2 || requestedCompound == 3;
            }

            return requestedCompound == 0 || requestedCompound == 1;
        }

        private static bool CanAttempt(int attemptCount, DateTime nextAttemptUtc)
        {
            if (attemptCount >= MaxAttemptsPerTarget)
            {
                return false;
            }

            return DateTime.UtcNow >= nextAttemptUtc;
        }

        private void ResetAttemptState()
        {
            _lastServiceTargetKey = string.Empty;
            _serviceTargetAttempts = 0;
            _serviceNextAttemptUtc = DateTime.MinValue;
            ResetCompoundAttempts();
        }

        private void ResetCompoundAttempts()
        {
            _lastCompoundTargetKey = string.Empty;
            _compoundTargetAttempts = 0;
            _compoundNextAttemptUtc = DateTime.MinValue;
        }

        private static string ModeToText(PitTyreControlMode mode)
        {
            switch (mode)
            {
                case PitTyreControlMode.Dry:
                    return "DRY";
                case PitTyreControlMode.Wet:
                    return "WET";
                case PitTyreControlMode.Auto:
                    return "AUTO";
                default:
                    return "OFF";
            }
        }
    }
}
