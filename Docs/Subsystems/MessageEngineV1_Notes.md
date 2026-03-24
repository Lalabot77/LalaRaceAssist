# Message Engine v1 (SimHub exports)

Validated against commit: 9f784a9
Last updated: 2026-03-24
Branch: work

## Purpose
Message Engine v1 is the dashboard-facing message selection/output layer for the v5 message catalog.
It decides which message is currently visible on each dash lane and exposes styling + stack telemetry via `MSGV1.*` exports.

> Status note: MSGV1 is still under active development and is **not** part of the shipped plugin v1.0 runtime contract.

## Core behavior
- The engine keeps an active stack of eligible messages.
- Active message selection is priority-first, then recency-aware within the same priority tier.
- Lala and Msg dash lanes each get their own active text/id/priority/style exports.
- Msg cancel (`MsgCxPressed`) is shared with legacy behavior for compatibility.

## Selection and clear logic (quick math/logic)
- **Priority ordering:** `High > Med > Low`.
- **Single press cancel:** remove the current top message; next eligible stack entry becomes active.
- **Repeated single press:** keeps walking down the stack until empty.
- **Double-tap clear:** if two presses arrive within ~350 ms, clear all active entries (`ClearAllPulse = true` briefly).

## Export contract

| Property | Description |
| --- | --- |
| `MSGV1.ActiveText_Lala` | Current message text chosen for the Lala dash (priority sorted). |
| `MSGV1.ActivePriority_Lala` | Priority label (`Low`/`Med`/`High`) for the Lala dash message. |
| `MSGV1.ActiveMsgId_Lala` | MsgId for the Lala dash message. |
| `MSGV1.ActiveText_Msg` | Current message text for the Msg dash. |
| `MSGV1.ActivePriority_Msg` | Priority label for the Msg dash message. |
| `MSGV1.ActiveMsgId_Msg` | MsgId for the Msg dash message. |
| `MSGV1.ActiveTextColor_Lala` | Text color for the Lala dash message (resolved from explicit or priority defaults). |
| `MSGV1.ActiveBgColor_Lala` | Background color for the Lala dash message. |
| `MSGV1.ActiveOutlineColor_Lala` | Outline color for the Lala dash message. |
| `MSGV1.ActiveFontSize_Lala` | Absolute font size for the Lala dash message. |
| `MSGV1.ActiveTextColor_Msg` | Text color for the Msg dash message. |
| `MSGV1.ActiveBgColor_Msg` | Background color for the Msg dash message. |
| `MSGV1.ActiveOutlineColor_Msg` | Outline color for the Msg dash message. |
| `MSGV1.ActiveFontSize_Msg` | Absolute font size for the Msg dash message. |
| `MSGV1.ActiveCount` | Number of active messages in the engine. |
| `MSGV1.LastCancelMsgId` | MsgId of the last message canceled via MsgCx. |
| `MSGV1.ClearAllPulse` | True for a short pulse when a double-tap clear occurs. |
| `MSGV1.StackCsv` | Debug view of the stack in most-recently-shown order (`msgId|Priority;...`). |

## Styling rules
- MessageDefinition JSON fields:
  - `TextColor`, `BgColor`, `OutlineColor` (`#AARRGGBB`, blank = use priority default)
  - `FontSize` (absolute; default 24)
- Priority defaults (when explicit style is blank):
  - High: red background, yellow text/outline
  - Med: yellow background, blue text/outline
  - Low: transparent background, white text/outline
- Flag messages can override background with the active flag color while keeping readable text/outline.

## Compatibility and migration notes
- Since MSGV1 is not shipped in plugin v1.0 yet, treat this page as forward-looking technical notes for test/dev builds.
- Legacy `MsgCxPressed` remains the control input; no new button wiring is required.
- Legacy `MSG.*` lanes remain exported for compatibility, but `MSGV1.*` is the canonical dash contract.
- `MSG.OtherClassBehindGap` is available; there is no `MSGOtherClassBehindGap` alias.
- Fuel "push OK" is one-shot per race session when no more fuel stops are required.
- Pit messages are mutually exclusive: `PIT_NOW` (`<= 0 laps`) overrides `PIT_SOON` (`< 2 laps`) and neither loops while state is unchanged.

## Diagnostics
- Missing evaluators are surfaced via stub handling (one-time log) and exported in `MSGV1.MissingEvaluatorsCsv` (e.g. `Eval_X|msg1,msg2`) so unresolved definitions are visible instead of silently skipped.
