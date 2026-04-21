using SimHub.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace LaunchPlugin
{
    internal enum PitCommandAction
    {
        ClearAll,
        ClearTyres,
        ToggleFuel,
        FuelSetZero,
        FuelAdd1,
        FuelRemove1,
        FuelAdd10,
        FuelRemove10,
        FuelSetMax,
        ToggleTyresAll,
        ToggleFastRepair,
        ToggleAutoFuel,
        Windshield
    }

    internal enum PitCommandTransportMode
    {
        Auto = 0,
        LegacyForegroundSendInput = 1,
        DirectMessageOnly = 2
    }

    internal sealed class PitCommandEngine
    {
        private const int FuelSetMaxCommandLitres = 150;
        private const string FuelSetZeroCommand = "#fuel 0.01$";
        private const int ConfirmationDelayMs = 180;
        private const int MessageHoldMs = 1500;
        private const uint KeyEventKeyUp = 0x0002;
        private const uint InputKeyboard = 1;
        private const uint KeyeventfUnicode = 0x0004;
        private const uint WmKeyDown = 0x0100;
        private const uint WmKeyUp = 0x0101;
        private const uint WmChar = 0x0102;

        private readonly HashSet<string> _onceWarnings = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private DateTime _messageUntilUtc = DateTime.MinValue;

        public string DisplayText { get; private set; } = string.Empty;
        public string LastAction { get; private set; } = string.Empty;
        public string LastRaw { get; private set; } = string.Empty;
        public bool FuelSetMaxToggleState { get; private set; }
        public bool Active => !string.IsNullOrWhiteSpace(DisplayText) && DateTime.UtcNow < _messageUntilUtc;

        public void Execute(PitCommandAction action, PluginManager pluginManager, double tankSpaceLitres, PitCommandTransportMode transportMode)
        {
            LastAction = action.ToString();
            if (action == PitCommandAction.FuelSetMax)
            {
                FuelSetMaxToggleState = !FuelSetMaxToggleState;
            }

            string raw = GetRawCommand(action);
            LastRaw = raw;
            string command = NormalizeChatCommand(raw);
            if (string.IsNullOrWhiteSpace(command))
            {
                PublishMessage("Pit Cmd Fail");
                WarnOnce("invalid_command_" + action, $"[LalaPlugin:PitCommand] action={action} failed: chat command mapping is empty.");
                return;
            }

            if (ShouldShortCircuitForTankFull(action) && tankSpaceLitres <= 0.05)
            {
                PublishMessage("Fuel MAX");
                SimHub.Logging.Current.Info($"[LalaPlugin:PitCommand] action={action} transport=chat-injection executed=false reason=tank-full tankSpaceL={tankSpaceLitres:F2}");
                return;
            }

            bool? before = ReadToggleState(action, pluginManager);
            string transportUsed;
            string reason;
            string fallbackFrom;
            bool transportAttempted = TryInjectChatCommand(command, transportMode, out transportUsed, out reason, out fallbackFrom);
            if (!transportAttempted)
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] action={action} transport={transportUsed} local-transport-issue reason={reason}{FormatFallbackSuffix(fallbackFrom)} raw='{raw}' normalized='{command}'");
                return;
            }

            bool confirmed = ConfirmAndPublishFeedback(action, pluginManager, before, tankSpaceLitres, transportUsed);
            SimHub.Logging.Current.Info($"[LalaPlugin:PitCommand] action={action} transport={transportUsed} attempted=true confirmed={confirmed} before={FormatNullable(before)} raw='{raw}' normalized='{command}'");
        }

        public bool ExecuteCustomMessage(string actionName, string messageText, string feedbackLabel, PitCommandTransportMode transportMode)
        {
            LastAction = string.IsNullOrWhiteSpace(actionName) ? "CustomMessage" : actionName.Trim();
            string normalized = NormalizeCustomMessage(messageText);
            LastRaw = normalized;
            if (string.IsNullOrWhiteSpace(normalized))
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn("[LalaPlugin:PitCommand] custom-message send blocked: message text is empty.");
                return false;
            }

            string transportUsed;
            string reason;
            string fallbackFrom;
            bool transportAttempted = TryInjectChatCommand(normalized, transportMode, out transportUsed, out reason, out fallbackFrom);
            if (!transportAttempted)
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] custom-message transport={transportUsed} local-transport-issue reason={reason}{FormatFallbackSuffix(fallbackFrom)} text='{normalized}'");
                return false;
            }

            PublishMessage(string.IsNullOrWhiteSpace(feedbackLabel) ? "Custom Msg" : feedbackLabel.Trim());
            SimHub.Logging.Current.Info($"[LalaPlugin:PitCommand] custom-message transport={transportUsed} attempted=true text='{normalized}'");
            return true;
        }

        public bool ExecuteRawPitCommand(string actionName, string rawCommandText, string feedbackLabel, PitCommandTransportMode transportMode)
        {
            LastAction = string.IsNullOrWhiteSpace(actionName) ? "PitRawCommand" : actionName.Trim();
            LastRaw = rawCommandText ?? string.Empty;

            string normalized = NormalizeChatCommand(rawCommandText);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn("[LalaPlugin:PitCommand] raw-command send blocked: command text is empty after normalization.");
                return false;
            }

            string transportUsed;
            string reason;
            string fallbackFrom;
            bool transportAttempted = TryInjectChatCommand(normalized, transportMode, out transportUsed, out reason, out fallbackFrom);
            if (!transportAttempted)
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] raw-command transport={transportUsed} local-transport-issue reason={reason}{FormatFallbackSuffix(fallbackFrom)} raw='{rawCommandText ?? string.Empty}' normalized='{normalized}'");
                return false;
            }

            PublishMessage(string.IsNullOrWhiteSpace(feedbackLabel) ? "Fuel Set" : feedbackLabel.Trim());
            SimHub.Logging.Current.Info($"[LalaPlugin:PitCommand] raw-command transport={transportUsed} attempted=true raw='{rawCommandText ?? string.Empty}' normalized='{normalized}'");
            return true;
        }

        public void PublishFeedback(string message)
        {
            PublishMessage(message);
        }

        public void PublishActionFeedback(string actionName, string message, string raw)
        {
            LastAction = string.IsNullOrWhiteSpace(actionName) ? "PitActionFeedback" : actionName.Trim();
            LastRaw = raw ?? string.Empty;
            PublishMessage(message);
        }

        private bool ConfirmAndPublishFeedback(PitCommandAction action, PluginManager pluginManager, bool? before, double tankSpaceLitres, string transportUsed)
        {
            if (!IsStatefulAction(action))
            {
                bool reachedFuelMax = WillFuelAddReachMax(action, tankSpaceLitres);
                PublishMessage(GetStatelessFeedback(action, reachedFuelMax));
                return true;
            }

            if (!before.HasValue)
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] action={action} expected-state-check skipped: before state unavailable.");
                return false;
            }

            Thread.Sleep(ConfirmationDelayMs);
            bool? after = ReadToggleState(action, pluginManager);
            bool expected = !before.Value;
            if (!after.HasValue || after.Value != expected)
            {
                PublishMessage("Pit Cmd Fail");
                SimHub.Logging.Current.Warn($"[LalaPlugin:PitCommand] action={action} expected-state-mismatch expected={expected} before={FormatNullable(before)} after={FormatNullable(after)} transport={transportUsed}");
                return false;
            }

            PublishMessage(GetToggleFeedback(action, after.Value));
            return true;
        }

        private static bool IsStatefulAction(PitCommandAction action)
        {
            switch (action)
            {
                case PitCommandAction.ToggleFuel:
                case PitCommandAction.ToggleTyresAll:
                case PitCommandAction.ToggleFastRepair:
                case PitCommandAction.ToggleAutoFuel:
                case PitCommandAction.Windshield:
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsFuelAddAction(PitCommandAction action)
        {
            return action == PitCommandAction.FuelAdd1 ||
                   action == PitCommandAction.FuelAdd10 ||
                   action == PitCommandAction.FuelSetMax;
        }

        private bool ShouldShortCircuitForTankFull(PitCommandAction action)
        {
            if (!IsFuelAddAction(action))
            {
                return false;
            }

            if (action == PitCommandAction.FuelSetMax)
            {
                return FuelSetMaxToggleState;
            }

            return true;
        }

        private static string NormalizeChatCommand(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return string.Empty;
            return raw.Trim().TrimEnd('$').Trim();
        }

        private static string NormalizeCustomMessage(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return string.Empty;
            }

            string normalized = raw.Replace("\r", " ").Replace("\n", " ").Trim();
            return normalized;
        }

        private bool WillFuelAddReachMax(PitCommandAction action, double tankSpaceLitres)
        {
            if (!IsFuelAddAction(action))
            {
                return false;
            }

            if (action == PitCommandAction.FuelSetMax)
            {
                return FuelSetMaxToggleState;
            }

            if (tankSpaceLitres <= 0.05)
            {
                return true;
            }

            double requestedAddLitres = action == PitCommandAction.FuelAdd10 ? 10.0 : 1.0;
            return tankSpaceLitres <= requestedAddLitres + 0.05;
        }

        private static string GetStatelessFeedback(PitCommandAction action, bool reachedFuelMax)
        {
            switch (action)
            {
                case PitCommandAction.ClearAll: return "Pit Clear All";
                case PitCommandAction.ClearTyres: return "Clear Tyres";
                case PitCommandAction.FuelSetZero: return "FUEL ZERO";
                case PitCommandAction.FuelAdd1: return reachedFuelMax ? "Fuel MAX" : "Fuel +1";
                case PitCommandAction.FuelRemove1: return "Fuel -1";
                case PitCommandAction.FuelAdd10: return reachedFuelMax ? "Fuel MAX" : "Fuel +10";
                case PitCommandAction.FuelRemove10: return "Fuel -10";
                case PitCommandAction.FuelSetMax: return reachedFuelMax ? "FUEL MAX" : "FUEL ZERO";
                default: return "Pit Cmd Fail";
            }
        }

        private string GetRawCommand(PitCommandAction action)
        {
            if (action == PitCommandAction.FuelSetMax)
            {
                return FuelSetMaxToggleState ? string.Format("#fuel +{0}$", FuelSetMaxCommandLitres) : FuelSetZeroCommand;
            }

            return GetRawCommandStatic(action);
        }

        private static string GetRawCommandStatic(PitCommandAction action)
        {
            switch (action)
            {
                case PitCommandAction.ClearAll: return "#clear$";
                case PitCommandAction.ClearTyres: return "#cleartires$";
                case PitCommandAction.ToggleFuel: return "#!fuel$";
                case PitCommandAction.FuelSetZero: return FuelSetZeroCommand;
                case PitCommandAction.FuelAdd1: return "#fuel +1$";
                case PitCommandAction.FuelRemove1: return "#fuel -1$";
                case PitCommandAction.FuelAdd10: return "#fuel +10$";
                case PitCommandAction.FuelRemove10: return "#fuel -10$";
                case PitCommandAction.ToggleTyresAll: return "#!t$";
                case PitCommandAction.ToggleFastRepair: return "#!fr$";
                case PitCommandAction.ToggleAutoFuel: return "#!autofuel$";
                case PitCommandAction.Windshield: return "#!ws$";
                default: return string.Empty;
            }
        }

        private static string GetToggleFeedback(PitCommandAction action, bool state)
        {
            switch (action)
            {
                case PitCommandAction.ToggleFuel:
                    return state ? "Fuel ON" : "Fuel OFF";
                case PitCommandAction.ToggleTyresAll:
                    return state ? "Tyres ON" : "Tyres OFF";
                case PitCommandAction.ToggleFastRepair:
                    return state ? "Fast Repair ON" : "Fast Repair OFF";
                case PitCommandAction.ToggleAutoFuel:
                    return state ? "Auto Fuel ON" : "Auto Fuel OFF";
                case PitCommandAction.Windshield:
                    return state ? "Tear-off ON" : "Tear-off OFF";
                default:
                    return "Pit Cmd Fail";
            }
        }

        private bool? ReadToggleState(PitCommandAction action, PluginManager pluginManager)
        {
            if (pluginManager == null)
            {
                return null;
            }

            switch (action)
            {
                case PitCommandAction.ToggleFuel:
                    return ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpFuelFill", null) ??
                           ReadPitServiceBit(pluginManager, 0x10);
                case PitCommandAction.ToggleTyresAll:
                    return ReadTyresAllState(pluginManager);
                case PitCommandAction.ToggleFastRepair:
                    return ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpFastRepair", null) ??
                           ReadPitServiceBit(pluginManager, 0x40);
                case PitCommandAction.ToggleAutoFuel:
                    return ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpAutoFuel", null);
                case PitCommandAction.Windshield:
                    return ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpWindshieldTearoff", null) ??
                           ReadPitServiceBit(pluginManager, 0x20);
                default:
                    return null;
            }
        }

        private bool? ReadTyresAllState(PluginManager pluginManager)
        {
            bool? lf = ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpLFTireChange", null);
            bool? rf = ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpRFTireChange", null);
            bool? lr = ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpLRTireChange", null);
            bool? rr = ReadBool(pluginManager, "DataCorePlugin.GameRawData.Telemetry.dpRRTireChange", null);

            if (lf.HasValue && rf.HasValue && lr.HasValue && rr.HasValue)
            {
                return lf.Value && rf.Value && lr.Value && rr.Value;
            }

            if (lf.HasValue)
            {
                return lf.Value;
            }

            return null;
        }

        private static bool? ReadPitServiceBit(PluginManager pluginManager, int bitMask)
        {
            try
            {
                object raw = pluginManager.GetPropertyValue("DataCorePlugin.GameRawData.Telemetry.PlayerCarPitSvStatus");
                if (raw == null)
                {
                    return null;
                }

                int status = Convert.ToInt32(raw);
                return (status & bitMask) != 0;
            }
            catch
            {
                return null;
            }
        }

        private static bool? ReadBool(PluginManager pluginManager, string propertyName, bool? fallback)
        {
            try
            {
                object raw = pluginManager.GetPropertyValue(propertyName);
                if (raw == null)
                {
                    return fallback;
                }

                return Convert.ToBoolean(raw);
            }
            catch
            {
                return fallback;
            }
        }

        private bool TryInjectChatCommand(string command, PitCommandTransportMode transportMode, out string transportUsed, out string reason, out string fallbackFrom)
        {
            transportUsed = "none";
            reason = "unknown";
            fallbackFrom = string.Empty;

            if (string.IsNullOrWhiteSpace(command))
            {
                reason = "empty-command";
                return false;
            }

            if (transportMode == PitCommandTransportMode.LegacyForegroundSendInput)
            {
                transportUsed = "sendinput";
                return TryForegroundSendInputChatCommand(command, out reason);
            }

            if (transportMode == PitCommandTransportMode.DirectMessageOnly)
            {
                transportUsed = "postmessage";
                return TryPostMessageChatCommand(command, out reason);
            }

            transportUsed = "postmessage";
            if (TryPostMessageChatCommand(command, out reason))
            {
                return true;
            }

            fallbackFrom = "postmessage";
            SimHub.Logging.Current.Info($"[LalaPlugin:PitCommand] transport=sendinput fallback_from=postmessage reason={reason}");
            transportUsed = "sendinput";
            return TryForegroundSendInputChatCommand(command, out reason);
        }

        private bool TryPostMessageChatCommand(string command, out string reason)
        {
            IntPtr iracingWindow;
            if (!TryResolveIracingMainWindow(out iracingWindow, out reason))
            {
                return false;
            }

            if (!PostVirtualKey(iracingWindow, Keys.T))
            {
                reason = "postmessage-open-chat-failed";
                return false;
            }

            Thread.Sleep(12);

            foreach (char c in command)
            {
                if (!PostMessage(iracingWindow, WmChar, (IntPtr)c, IntPtr.Zero))
                {
                    reason = "postmessage-char-failed";
                    return false;
                }
            }

            Thread.Sleep(8);
            if (!PostVirtualKey(iracingWindow, Keys.Enter))
            {
                reason = "postmessage-submit-failed";
                return false;
            }

            reason = "none";
            return true;
        }

        private bool TryForegroundSendInputChatCommand(string command, out string reason)
        {
            if (!IsIracingForeground())
            {
                reason = "not-foreground";
                WarnOnce("not_foreground", "[LalaPlugin:PitCommand] transport=sendinput unavailable reason=not-foreground.");
                return false;
            }

            TapVirtualKey(Keys.T);
            Thread.Sleep(40);

            if (!SendUnicodeText(command))
            {
                reason = "unicode-send-failed";
                WarnOnce("type_failed", "[LalaPlugin:PitCommand] transport=sendinput local submission issue while sending command text (transport attempt unconfirmed).");
                return false;
            }

            Thread.Sleep(20);
            TapVirtualKey(Keys.Enter);
            reason = "none";
            return true;
        }

        private static bool PostVirtualKey(IntPtr hwnd, Keys key)
        {
            if (hwnd == IntPtr.Zero)
            {
                return false;
            }

            int vk = (int)key;
            return PostMessage(hwnd, WmKeyDown, (IntPtr)vk, IntPtr.Zero) &&
                   PostMessage(hwnd, WmKeyUp, (IntPtr)vk, IntPtr.Zero);
        }

        private static void TapVirtualKey(Keys key)
        {
            byte vk = (byte)key;
            keybd_event(vk, 0, 0, UIntPtr.Zero);
            Thread.Sleep(12);
            keybd_event(vk, 0, KeyEventKeyUp, UIntPtr.Zero);
        }

        private static bool SendUnicodeText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            foreach (char c in text)
            {
                if (!SendUnicodeChar(c))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SendUnicodeChar(char c)
        {
            INPUT[] inputs = new INPUT[2];
            inputs[0] = CreateUnicodeInput(c, false);
            inputs[1] = CreateUnicodeInput(c, true);
            uint sent = SendInput((uint)inputs.Length, inputs, Marshal.SizeOf(typeof(INPUT)));
            return sent == inputs.Length;
        }

        private static INPUT CreateUnicodeInput(char c, bool keyUp)
        {
            INPUT input = new INPUT();
            input.type = InputKeyboard;
            input.U.ki.wVk = 0;
            input.U.ki.wScan = c;
            input.U.ki.dwFlags = KeyeventfUnicode | (keyUp ? KeyEventKeyUp : 0u);
            input.U.ki.time = 0;
            input.U.ki.dwExtraInfo = IntPtr.Zero;
            return input;
        }

        private static bool IsIracingForeground()
        {
            try
            {
                IntPtr hwnd = GetForegroundWindow();
                if (hwnd == IntPtr.Zero)
                {
                    return false;
                }

                uint pid;
                GetWindowThreadProcessId(hwnd, out pid);
                if (pid == 0)
                {
                    return false;
                }

                using (Process p = Process.GetProcessById((int)pid))
                {
                    return IsIracingProcessName(p.ProcessName);
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool TryResolveIracingMainWindow(out IntPtr windowHandle, out string reason)
        {
            windowHandle = IntPtr.Zero;
            reason = "unknown";

            Process[] processes;
            try
            {
                processes = Process.GetProcesses();
            }
            catch
            {
                reason = "process-enumeration-failed";
                return false;
            }

            bool sawIracingProcess = false;
            foreach (Process process in processes)
            {
                try
                {
                    if (!IsIracingProcessName(process.ProcessName))
                    {
                        continue;
                    }

                    sawIracingProcess = true;
                    if (process.MainWindowHandle != IntPtr.Zero)
                    {
                        windowHandle = process.MainWindowHandle;
                        reason = "none";
                        return true;
                    }
                }
                catch
                {
                    // keep searching
                }
            }

            reason = sawIracingProcess ? "no-iracing-window" : "no-iracing-process";
            return false;
        }

        private static bool IsIracingProcessName(string processName)
        {
            if (string.IsNullOrWhiteSpace(processName))
            {
                return false;
            }

            return string.Equals(processName, "iRacingSim64DX11", StringComparison.OrdinalIgnoreCase);
        }

        private void PublishMessage(string message)
        {
            DisplayText = message ?? string.Empty;
            _messageUntilUtc = DateTime.UtcNow.AddMilliseconds(MessageHoldMs);
        }

        private void WarnOnce(string key, string message)
        {
            if (_onceWarnings.Contains(key))
            {
                return;
            }

            _onceWarnings.Add(key);
            SimHub.Logging.Current.Warn(message);
        }

        private static string FormatNullable(bool? value)
        {
            return value.HasValue ? value.Value.ToString() : "n/a";
        }

        private static string FormatFallbackSuffix(string fallbackFrom)
        {
            return string.IsNullOrWhiteSpace(fallbackFrom) ? string.Empty : $" fallback_from={fallbackFrom}";
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct INPUT
        {
            public uint type;
            public InputUnion U;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct InputUnion
        {
            [FieldOffset(0)]
            public MOUSEINPUT mi;

            [FieldOffset(0)]
            public KEYBDINPUT ki;

            [FieldOffset(0)]
            public HARDWAREINPUT hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MOUSEINPUT
        {
            public int dx;
            public int dy;
            public uint mouseData;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct KEYBDINPUT
        {
            public ushort wVk;
            public ushort wScan;
            public uint dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct HARDWAREINPUT
        {
            public uint uMsg;
            public ushort wParamL;
            public ushort wParamH;
        }
    }
}
