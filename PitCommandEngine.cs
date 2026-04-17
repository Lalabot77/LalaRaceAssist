using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LaunchPlugin
{
    internal enum PitCommandAction
    {
        ClearAll,
        FuelAdd,
        FuelRemove,
        ToggleFuel,
        ToggleTiresAll,
        ToggleFastRepair
    }

    internal sealed class PitCommandEngine
    {
        private const string TransportModeSdk = "sdk";
        private const string TransportModeMacroHotkey = "macro_hotkey";
        private const uint KeyEventKeyUp = 0x0002;

        private readonly Func<LaunchPluginSettings> _settingsProvider;
        private bool _sdkUnavailableLogged;
        private readonly HashSet<string> _missingBindingWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public PitCommandEngine(Func<LaunchPluginSettings> settingsProvider)
        {
            _settingsProvider = settingsProvider ?? throw new ArgumentNullException(nameof(settingsProvider));
        }

        public string ActiveTransportModeLabel
        {
            get
            {
                var mode = NormalizeMode(_settingsProvider()?.PitCommandTransportMode);
                return string.Equals(mode, TransportModeSdk, StringComparison.Ordinal) ? "sdk-requested-fallback-macro-hotkey" : "macro-hotkey";
            }
        }

        public void Execute(PitCommandAction action, PluginManager pluginManager)
        {
            if (pluginManager == null)
            {
                SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] action={action} ignored: PluginManager unavailable.");
                return;
            }

            string configuredMode = NormalizeMode(_settingsProvider()?.PitCommandTransportMode);
            bool executed = false;
            string transportUsed = ActiveTransportModeLabel;

            if (string.Equals(configuredMode, TransportModeSdk, StringComparison.Ordinal))
            {
                executed = TryExecuteViaSdk(action, pluginManager);
                transportUsed = executed ? "sdk" : "sdk-requested-fallback-macro-hotkey";
            }

            if (!executed)
            {
                executed = TryExecuteViaMacroHotkey(action);
                transportUsed = executed ? transportUsed : "none";
            }

            SimHub.Logging.Current.Info($"[LalaPlugin:PitCommand] action={action} transport={transportUsed} executed={executed}");
        }

        private bool TryExecuteViaSdk(PitCommandAction action, PluginManager pluginManager)
        {
            if (!_sdkUnavailableLogged)
            {
                _sdkUnavailableLogged = true;
                SimHub.Logging.Current.Warn("[LalaPlugin:PitCommand] SDK transport requested but no writable iRacing pit-command API seam exists in current plugin references; falling back to macro hotkeys.");
            }

            return false;
        }

        private bool TryExecuteViaMacroHotkey(PitCommandAction action)
        {
            string keyBinding = ResolveMacroHotkey(action);
            if (string.IsNullOrWhiteSpace(keyBinding))
            {
                WarnMissingBinding(action, keyBinding);
                return false;
            }

            if (!TryParseKey(keyBinding, out Keys key))
            {
                WarnMissingBinding(action, keyBinding);
                return false;
            }

            TapKey(key);
            return true;
        }

        private string ResolveMacroHotkey(PitCommandAction action)
        {
            var settings = _settingsProvider();
            if (settings == null)
            {
                return string.Empty;
            }

            switch (action)
            {
                case PitCommandAction.ClearAll:
                    return settings.PitMacroKeyClearAll;
                case PitCommandAction.FuelAdd:
                    return settings.PitMacroKeyFuelAdd;
                case PitCommandAction.FuelRemove:
                    return settings.PitMacroKeyFuelRemove;
                case PitCommandAction.ToggleFuel:
                    return settings.PitMacroKeyToggleFuel;
                case PitCommandAction.ToggleTiresAll:
                    return settings.PitMacroKeyToggleTiresAll;
                case PitCommandAction.ToggleFastRepair:
                    return settings.PitMacroKeyToggleFastRepair;
                default:
                    return string.Empty;
            }
        }

        private static bool TryParseKey(string keyBinding, out Keys key)
        {
            key = Keys.None;
            if (string.IsNullOrWhiteSpace(keyBinding))
            {
                return false;
            }

            return Enum.TryParse(keyBinding.Trim(), true, out key) && key != Keys.None;
        }

        private void WarnMissingBinding(PitCommandAction action, string rawBinding)
        {
            string normalized = (rawBinding ?? string.Empty).Trim();
            string warningKey = action + "|" + normalized;
            if (_missingBindingWarnings.Contains(warningKey))
            {
                return;
            }

            _missingBindingWarnings.Add(warningKey);
            SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] action={action} macro hotkey binding invalid or missing ('{rawBinding}'). Configure LaunchPluginSettings pit macro key fields and bind matching iRacing pit macros.");
        }

        private static void TapKey(Keys key)
        {
            byte vk = (byte)key;
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            Thread.Sleep(20);
            keybd_event(vk, 0, KeyEventKeyUp, UIntPtr.Zero);
        }

        private static string NormalizeMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
            {
                return TransportModeMacroHotkey;
            }

            string trimmed = mode.Trim().Replace('-', '_');
            if (string.Equals(trimmed, "macro", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(trimmed, "macrohotkey", StringComparison.OrdinalIgnoreCase))
            {
                return TransportModeMacroHotkey;
            }

            return trimmed.ToLowerInvariant();
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
    }
}
