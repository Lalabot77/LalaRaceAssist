# Repo Status

- 2026-06-03: Fuel burn target SESSION pit-stop fuel credit landed.
  - `Fuel.Burn.Target` SESSION now adds a fixed 40.0 seconds of fuel-burn credit per conservative calculated remaining stop before dividing by `Fuel.LiveLapsRemainingInRace_Stable`.
  - Remaining stops use the existing validated `Fuel.Live.RemainingStints` exact runtime refuel basis rounded up; burn rate uses existing runtime refuel selected burn and selected projection-lap seam. Invalid/non-positive credit inputs apply no credit and preserve prior SESSION behavior.
  - Preserved STINT, END, INVALID, `Fuel.Burn.TargetText`, `Fuel.StintBurnTarget`, `Fuel.RequiredBurnToEnd`, `Fuel.Refuel.*`, remaining-stints exports, pit/refuel math, pit-loss learning, pit-window logic, Strategy planner, dashboard JSON, and XAML.
  - Property Snapshot list reviewed: yes, no group change required because existing `Fuel.Burn.*` exports remain covered by the `Fuel.*` Fuel/Strategy prefix.

- 2026-06-03: Fuel burn target selector landed.
  - Added plugin-owned `Fuel.Burn.Target`, `Fuel.Burn.TargetText`, and `Fuel.Burn.TargetValid` dashboard exports.
  - SESSION uses the raw driver-selected MFD fuel request seam (`PitSvFuel`, gated by `dpFuelFill`) before tank-space clamp and explicitly does not use `Fuel.Refuel.NextLitresCeil`, `Fuel.Pit.WillAdd`, plugin recommendations, planner fuel values, or tank-space-clamped add values.
  - Invalid/reset `Fuel.Live.RemainingStints=0` is not interpreted as END; the selector requires the internal remaining-stints validity basis and publishes `INVALID` when that basis is unavailable.
  - Review follow-up: missing/unreadable `PitSvFuel` now invalidates only SESSION; STINT and END still publish from their own validated basis when the raw MFD request is unavailable.
  - END is guarded against reserve/contingency cases that still require another stop.
  - Preserved `Fuel.StintBurnTarget`, `Fuel.RequiredBurnToEnd`, `Fuel.Refuel.*`, pit/refuel math, dashboard JSON, and XAML.
  - Property Snapshot list reviewed: yes; new `Fuel.Burn.*` exports route through the existing `Fuel.*` prefix into the Fuel/Strategy group.

Validated against commit: HEAD
Last updated: 2026-06-03
Branch: work

## Current status
- 2026-06-03: Pit Fuel Control stale request-fault expiry and MsgCx recovery landed.
  - Added a bounded confirmation expiry for plugin-owned requested-fuel expectations so a manual MFD fuel amount change absorbed during the post-send suppression window is treated as external/manual takeover instead of latching `Pit.FuelControl.Fault=2` indefinitely.
  - MsgCx now invokes a no-command Pit Fuel Control recovery check after existing message-system notifications; when a pending request is already stale/expired by the confirmation-expiry rules, it routes through the same external/manual takeover handling as telemetry expiry, preserving live pending-command ownership, DATA/SOURCE/MODE semantics, and pit command transport behavior.
  - `Pit.FuelControl.Fault` values remain `0/1/2/3`; no exports were added, removed, or renamed.
  - Property Snapshot list reviewed: yes, no group change required because the existing `Pit.*` prefix still routes `Pit.FuelControl.*` into Pit/PitExit.

- 2026-06-02: Fuel burn analysis Avg10, minimum burn, and remaining-laps range support landed.
  - Added `Fuel.Burn.Analysis.Avg10`, `MinObserved`, `RemainingLapsMin`, and `RemainingLapsMax`, plus independent `BurnAnalysisResetMinObserved`.
  - Avg10 reuses the fresh accepted-lap observer with partial-window behavior and a 10-sample rolling list; Avg3/Avg5 continue to average only their latest 3/5 samples. `MinObserved` records only fresh accepted samples and remains independent from `MaxObserved`.
  - Remaining-laps bounds reuse the existing runtime current-fuel cache under the burn-analysis synchronization strategy: conservative `current fuel / MaxObserved`, optimistic `current fuel / MinObserved`, with safe `0.0` publication for invalid, empty, non-finite, or non-positive inputs.
  - Preserved accepted-lap gating, seeded-value exclusion, scoped sample counts, pit-exit stint reset, lifecycle reset behavior, `Fuel.LiveFuelPerLap*`, predictor, Strategy, pit/refuel math, dashboard JSON, and XAML.
  - Property Snapshot list reviewed: yes; all new `Fuel.Burn.Analysis.*` exports continue to route through the existing `Fuel.*` prefix into the Fuel/Strategy group.

- 2026-06-01: PR #770 current-tick checkpoint gating and precision filtered-expiry follow-up landed.
  - Direct checkpoint eligibility is refreshed from current `CarIdxTrackSurface` / `CarIdxOnPitRoad` telemetry immediately before both Opponents publication paths and synchronized during normal CarSA update, removing the prior-tick pit-entry/off-track seam window.
  - Ineligible direct caches still clear fail-closed; eligible-car 15-second lookup and same-lap/adjacent-lap math remain unchanged.
  - Slot-01 precision filtered fallback now expires when the last truth observation exceeds the existing freshness limit, retaining `fresh truth -> recently defensible filtered -> track fallback -> invalid` without sticky hold.
  - Preserved CarSA physical slots, `Gap.RelativeSec`, Opponents ordering/fallback, H2H, dashboards, JSON, and export names.
  - Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.

- 2026-06-01: CarSA direct checkpoint seam pit-state gating landed.
  - `TryGetCheckpointGapSec(...)` now requires both current cars to be on-track, not on pit road, and explicitly `TrackSurfaceRaw == OnTrack`; pit lane, pit stall/tow, off-track, NotInWorld, unknown, and invalid states fail closed to the existing downstream fallback.
  - Ineligible cars clear their narrow direct-checkpoint timestamp/lap arrays and do not record new direct timestamps until eligible, preventing pre-entry timestamp reuse immediately after pit exit.
  - Preserved the existing 15-second rule for eligible cars, same-lap/adjacent-lap correction math, CarSA physical slots, `Gap.RelativeSec`, slot-01 precision gaps, Opponents/H2H behavior, dashboards, and export names.
  - Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.

- 2026-06-01: CarSA slot-01 precision-gap freshness fix landed.
  - `Car.Ahead01P.Gap.Sec` / `Car.Behind01P.Gap.Sec` now require gate truth within the existing freshness limit instead of publishing stale raw truth indefinitely.
  - Precision gaps preserve sharper textual/number-display behavior with `fresh truth -> defensible filtered -> track fallback -> invalid`, without adopting `Gap.RelativeSec` sticky hold or changing RelativeSec publication.
  - Preserved physical slot selection, Opponents/H2H behavior, dashboard contracts, and all export names.
  - Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.

- 2026-06-01: CarSA checkpoint adjacent-lap runtime-scale fix landed.
  - `TryGetCheckpointGapSec(...)` now uses a CarSA runtime-owned lap-time scale for adjacent-lap correction instead of reading `_outputs.Debug.LapTimeUsedSec`.
  - The runtime scale is assigned from the existing `SelectLapTimeUsed(...)` result independently of System Debug state and is cleared with the existing gate-gap caches.
  - Preserved `SelectLapTimeUsed(...)` order, adjacent-lap formula/sign and `abs(lapDeltaAtGate) <= 1` guard, same-lap behavior, invalid-scale return-false behavior, native downstream fallback, subsystem ownership, and all export names.
  - Property Snapshot list reviewed: yes, no group change required because no exports were added, removed, renamed, or re-grouped.
- 2026-06-01: Fuel burn analysis sample-count split landed.
  - Added `Fuel.Burn.Analysis.AvgSampleCount`, `StintSampleCount`, and `SessionSampleCount` so dashboard readiness colouring can use the matching rolling-average, current-stint, or session-average context. Existing `Fuel.Burn.Analysis.SampleCount` remains present as a compatibility alias of `SessionSampleCount`.
  - Reused the existing synchronized burn-analysis backing state and dedicated lock. Accepted-lap gating, value calculations, scoped reset behavior, Fuel Model lifecycle reset behavior, `Fuel.LiveFuelPerLap*`, predictor, Strategy, pit/refuel math, dashboard JSON, and XAML remain unchanged.
  - Property Snapshot list reviewed: yes; all new `Fuel.Burn.Analysis.*` exports continue to route through the existing `Fuel.*` prefix into the Fuel/Strategy group.

- 2026-06-01: Fuel burn analysis popup review follow-up landed.
  - Added brief public dashboard/fuel-model guidance for `LalaLaunch.Fuel.Burn.DisplayAnalysis`, `LalaLaunch.BurnDisplayToggle`, optional reset actions, and direct `LalaLaunch.Fuel.Burn.Analysis.*` consumption.
  - Synchronized all `Fuel.Burn.Analysis.*` backing state with one dedicated lock across accepted-sample recording, scoped resets, lifecycle reset, and property reads. Aggregate pairs (`CurrentStint`, `SessionAvg`/`SampleCount`) cannot be observed mid-update or mid-reset. Existing partial-window averaging, reset semantics, acceptance logic, fuel math, Strategy, pit/refuel behavior, dashboard JSON, and XAML remain unchanged.

- 2026-06-01: Fuel burn analysis popup plugin support landed.
  - Added presentation export `Fuel.Burn.DisplayAnalysis`, plugin toggle action `LalaLaunch.BurnDisplayToggle`, and fresh accepted-lap analysis exports `Fuel.Burn.Analysis.LastLap`, `Avg3`, `Avg5`, `CurrentStint`, `SessionAvg`, `MaxObserved`, and `SampleCount`.
  - Added independently scoped reset actions for rolling averages, current stint, session average/sample count, and max observed. `CurrentStint` also resets on the existing confirmed pit-exit edge; Fuel Model lifecycle resets clear all analysis values. Fresh accepted laps only are observed across wet/dry, and seeded race-start model values remain excluded.
  - Preserved existing accepted-lap checks, `Fuel.LiveFuelPerLap*`, `Fuel.FuelBurnPredictor*`, Strategy planner, pit/refuel math, dashboard JSON, and XAML. Property Snapshot list reviewed: yes; all new `Fuel.Burn.*` exports resolve through the existing `Fuel.*` prefix into the Fuel/Strategy group.

- 2026-05-31: Opponents CarSA checkpoint seam same-tick overwrite wiring fix validated.
  - The ordinary `UpdateLiveProperties(...)` Opponents refresh now passes the CarSA `TryGetCheckpointGapSec` delegate when CarSA is available, so valid `Opp.Ahead1` / `Opp.Behind1` preferred checkpoint gaps are no longer replaced by same-tick native progress fallback before export.
  - Preserved duplicate refresh timing, native fallback behavior, Opponents ordering, pit-exit behavior, League Class matching, H2H selector ownership, and all export names.
  - Property Snapshot list reviewed: yes, no group change required because existing `Opp.*` exports remain in the same `CarOppH2H` group and no property names changed.

- 2026-05-31: PR #767 PreRace planner-authority review follow-up landed.
  - Manual Dry/Wet planner authority now requires a known matching live condition; unknown live condition falls back safely.
  - Timed authority now applies a strict `0.01`-minute PreRace-specific tolerance alongside the existing strict `0.001`-lap tolerance without changing the shared coarse planner/live comparison helper.
  - PreRace authority matching now consumes the already resolved Live Detect race-definition seam (`IsLimitedSessionLaps` / `IsLimitedTime`, compatibility fields, and `SessionsXX` fallback) instead of rebuilding from raw underscore fields. Existing fallback math, formation behavior, runtime pit/refuel families, and dashboard JSON remain unchanged.
  - Property Snapshot list reviewed: yes (behavior refinement stays within existing `PreRace.*` and indirectly affected `StrategyDash.*` Fuel/Strategy groups; no export list change required).

- 2026-05-30: PreRace total-fuel planner authority gate landed.
  - `LalaLaunch.PreRace.TotalFuelNeeded` now anchors to `FuelCalculator.TotalFuelNeeded - FormationFuelPlanned + FormationFuelRemaining` only when planner total, car, canonical track key, race basis, race length, and manually forced wet/dry gates match live session context; lap-limited authority uses a strict `0.001`-lap tolerance.
  - When any hard gate fails, `PreRace.TotalFuelNeeded` retains the existing live/session fallback calculation (`base race fuel + active contingency + FormationFuelRemaining`).
  - `FormationFuelPlanned`, `FormationFuelRemaining`, `Fuel.Pit.TotalNeededToEnd`, `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Pit.FuelControl.*`, runtime pit/refuel behavior, and dashboard JSON files remain unchanged. Existing `StrategyDash.*` start-fuel helpers inherit the corrected PreRace total through their existing adapter path.
  - Property Snapshot list reviewed: yes (`PreRace.*` and indirectly affected `StrategyDash.*` remain covered by the existing Fuel/Strategy group prefixes; no export list change required).

- 2026-05-29: PreRace formation fuel exports and total-fuel refinement landed.
  - Added `LalaLaunch.PreRace.FormationFuelPlanned` and `LalaLaunch.PreRace.FormationFuelRemaining` for planner allowance vs remaining formation allowance.
  - `LalaLaunch.PreRace.TotalFuelNeeded` now consumes `FormationFuelRemaining` (`base race fuel + active contingency + remaining formation fuel`) and uses runtime after-zero when available with planner after-zero fallback for timed PreRace projection.
  - `Fuel.Pit.TotalNeededToEnd`, `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, pit command/send behavior, and dashboard JSON files remain unchanged.
  - Property Snapshot list reviewed: yes (`PreRace.*` remains covered by the existing Fuel/Strategy group prefix).

- 2026-05-29: Double-prefix LalaLaunch export registration cleanup landed.
  - PreRace and Friends exports now register internally as `PreRace.*` / `Friends.Count`, preventing accidental public `LalaLaunch.LalaLaunch.*` names while preserving intended public dashboard names (`LalaLaunch.PreRace.*`, `LalaLaunch.Friends.Count`).
  - No dashboard package JSON files were edited; existing double-prefix dash bindings remain manual Dash Studio fixes.
  - Property Snapshot list reviewed: yes (`PreRace.*` maps to Fuel/Strategy; `Friends.*` maps to Car/Opp/H2H).

- 2026-05-29: iRacingExtraProperties dependency audit completed (documentation-only).
  - Added `Docs/Internal/iRacingExtraProperties_Dependency_Audit.md` with code/docs findings, replacement map, and a dashboard manual-fix report for packaged `.simhubdash` references.
  - No C# runtime dependency on optional `IRacingExtraProperties` reads was found; remaining code hits are bounded warnings/comments documenting removed fallbacks.
  - Dashboard packages were audited but intentionally not modified.
  - Property Snapshot list reviewed: yes (no SimHub export/property add/remove/rename/behavior change).

- 2026-05-28: Strategy Dash Advanced/Simple mode binding landed.
  - Added plugin action `StrategyDash.ModeToggle` for SimHub Controls & Events / Dash Studio bindings.
  - Added persisted dashboard status exports `LalaLaunch.StrategyDash.AdvancedMode` (`true` = Advanced, `false` = Simple) and `LalaLaunch.StrategyDash.ModeText` (`ADVANCED`/`SIMPLE`).
  - Strategy/fuel calculations, StrategyDash advice math, PreRace behavior, telemetry timing, and dashboard navigation are unchanged; the new seam is presentation-mode state only.
  - Property Snapshot list reviewed: yes (new `StrategyDash.*` exports are covered by the existing Fuel/Strategy snapshot group prefix).

- 2026-05-28: League-aware `Car.iRatingSOF` cohort update validated.
  - League Class enabled + resolved player effective class: SOF now averages positive iRating values for current-session `DriverInfo.Drivers##` rows in the player effective subclass cohort.
  - League Class disabled/unresolved fallback preserved: SOF remains full-field positive iRating average.
  - Property Snapshot list reviewed: yes (no export add/remove/rename; behavior-only update for existing `Car.iRatingSOF`).

- Documentation alignment update completed for Fuel Revamp Phase 3B temporal semantics/lifecycle classification (documentation-only).
- Runtime behavior unchanged in this pass: no code edits, no export additions/removals/renames, no calculation changes.
- Canonical seam split remains unchanged: `Fuel.Refuel.*` runtime tactical guidance vs `StrategyDash.*`/`PreRace.*` pre-green planning guidance.
- Property Snapshot list reviewed: yes (no export/property behavior change required).
- PR #759 review-comment follow-up completed: corrected documentation export names to canonical SimHub property names (`Fuel.ProjectionLapTime_Stable*`, `LalaLaunch.PreRace.*`, `Fuel.Live.DriveTimeAfterZero`, `Fuel.Live.ProjectedDriveSecondsRemaining`).

- 2026-05-27: Pit window contingency-awareness alignment landed (runtime fuel status semantics).
  - Pit-window open feasibility (`CLEAR PUSH`/`RACE PACE`/`FUEL SAVE`) now evaluates contingency-aware required add by mode (`lapsRemaining*burn + contingencyForMode - currentFuel`) before tank-space fit checks.
  - Litre-configured contingency applies shared litres across modes; lap-configured contingency resolves litres per mode burn basis (push/stable/save).
  - Preserved invariants: no changes to `Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.Pit.NeedToAdd`, `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`, `Fuel.Pit.Box.*`, pit-box distance/time logic, `Pit.FuelControl.*`, or command send behavior.
  - PR #758 follow-up: base no-stop (`PitStopsRequiredByFuel<=0`) no longer forces pit-window `N/A` when reserve-protected shortfall exists; reserve-short cases now continue into contingency-aware pit-window feasibility states.

- 2026-05-27: PreRace strategy-status phase cleanup (next-stint advisory split) validated.
  - Main `LalaLaunch.PreRace.StatusText` for required one-stop now keeps strategy/start-fuel validity focus only during pre-grid/gridding: under-fuelled and next-stint-check branches publish `SINGLE STOP POSSIBLE` (orange) in Phase 1/2, while Phase 3/5 retains next-stint advisory text (`2 STINT PLAN REQUIRES MORE FUEL` / `CHECK NEXT STINT FUEL`).
  - `OVERFUELLED` remains unchanged/actionable when excess exceeds contingency threshold; one-stop feasibility hard-stop (`2 STINT PLAN NOT POSSIBLE`) remains unchanged.
  - `StrategyDash.NextRefuel*` target/delta/advice/status math and all runtime tactical families (`Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.Pit.*`, `Pit.FuelControl.*`, `Fuel.RequiredBurnToEnd*`) remain unchanged.

- 2026-05-20: Documentation sweep for release readiness completed (documentation-only).
  - Added canonical internal workflow doc `Docs/Internal/Property_Snapshot_Debug_Workflow.md` for manual/rolling/replay snapshot operations, troubleshooting matrix, and escalation guidance.
  - Clarified Project Index topic ownership, terminology mapping, and release-history ownership rubric.
  - Clarified Fuel/Strategy/PreRace/runtime boundaries across user docs and subsystem references.
  - RaceFinish ownership explicitly kept distributed (no new `Docs/Subsystems/RaceFinish.md`).
  - No runtime files, exports, Property Snapshot grouping, log strings, or behavior changed.

- 2026-05-20: Strategy Save All ownership fix landed.
  - Strategy `Save All to Profile` now saves strictly to planner-selected `SelectedCarProfile` + selected planner track (`SelectedTrackStats`/selected track key), including when a different live session car/track is active.
  - Save path no longer retargets to live `ActiveProfile` / `CurrentTrackKey`; confirmation messaging now reflects the true planner-selected save target.
  - `Apply to Live Session` remains separate live-action ownership; no runtime fuel/pit authority changes.
- 2026-05-20: Early pre-grid restricted max-tank authority seam validated.
  - Runtime max-tank detection now prefers `DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverCarFuelMaxLtr * DriverCarMaxFuelPct` when both are valid, so restricted caps can publish before `GameData.MaxFuel`/`CarSettings_MaxFUEL` hydrate.
  - Added defensive normalization for `DriverCarMaxFuelPct` to support both fractional (`0..1`) and percent (`0..100`) inputs.
  - `Fuel.Setup.FuelLevel` semantics remain unchanged and are not used as max-tank authority.
- 2026-05-20: PR #752 follow-up applied before merge.
  - Pending live context now arms before profile/track selection loads and promotes to canonical resolved track key before track-triggered reload.
  - Combo change detection now compares canonical resolved live track key, so same-session key/display mismatches no longer retrigger new-combo snapshot clears.
  - No-profile SimHub/Default fuel fallback now preserves dry-equivalent basis for wet-factor stability (no double wet-factor application).

- 2026-05-20: PR #748 follow-up #3 (planner fallback edge cases) completed.
  - Live-session pending/active track identity for planner SimHub fallback guard now uses canonical resolved track key precedence (`TrackStats.Key` -> `CurrentTrackKey` -> display name).
  - No-profile fuel fallback (`SimHub`/`Default`) now seeds planner dry-basis for wet refresh stability.
  - Pit-lane loss assignment now decoupled from dry-fuel availability and reset-safe for no-profile track switches (no stale carry-over).

- 2026-05-19: PR #748 follow-up #2 validated (first live-load SimHub guard window + wet basis preservation).
  - Strategy planner SimHub fallback guard now accepts pending live identity during `ApplyLiveSession` pre-activation window, enabling immediate first live no-profile fallback (`SimHub est`/`SimHub`) when identities match.
  - Stale/disconnected/unmatched SimHub cached fallback remains blocked (no active/pending identity match -> fallback to Default paths).
  - Missing dry avg no longer zeroes wet-usable basis when wet avg exists; wet profile fuel now remains stable across wet-factor/condition refresh paths.

- 2026-05-19: PR #748 follow-up validated (Strategy fallback branch/guard fixes).
  - Fixed Strategy Profile fuel fallback branch placement so no-profile tracks now execute `PROFILE -> SIMHUB -> DEFAULT` even when dry profile avg is missing.
  - Added live-context guard to planner SimHub fallbacks (lap+fuel): requires active live session and planner selected car/track identity match; otherwise SimHub fallback is blocked and defaults apply.
  - Prevents stale/disconnected cached SimHub/DataCore values from appearing as planner fallback inputs.

- 2026-05-19: Strategy planner SimHub fallback cleanup + DATA LIVE lap SIM parity validated.
  - Strategy Profile-mode no-profile load now uses truthful fallback chains (`Lap: PROFILE -> SIMHUB EST -> DEFAULT`, `Fuel: PROFILE -> SIMHUB -> DEFAULT`) and no longer surfaces stale/manual-looking lap labels on auto load.
  - Strategy source helper text now aligns with actual fallback provenance (`SimHub est`/`SimHub`/`Default`); default 2.8 fuel fallback no longer reports `profile`.
  - Runtime DATA LIVE lap SIM fallback now uses `DataCorePlugin.GameRawData.SessionData.DriverInfo.DriverCarEstLapTime`, enabling `DATA LIVE | LAP SIM / BURN SIM` when both SimHub fallbacks are available.
  - DATA SAVED authority contract unchanged.
- 2026-05-19: PR #750 follow-up (Option B) validated — live tyre prediction decoupled from Strategy slider.
  - `Fuel.Live.TireChangeTime_S` full-4 basis now reads active profile `TireChangeTime` (sanitized non-negative) instead of planner slider value.
  - Strategy slider remains planner what-if only (still seeded/refreshed from profile), and no longer mutates live boxed tyre-time prediction basis.

- 2026-05-19: Tyre timing refresh + boxed tyre-count latch + preset checkbox-state follow-up validated.
  - Strategy tyre-time basis now refreshes from active profile tyre time on profile load/switch and active profile tyre-learn updates (no manual slider move needed).
  - Boxed pit target modeling now latches pre-service selected tyre count on in-box activation; in-service DP tyre-flag clear-down no longer reduces active boxed `Pit.Box.TargetSec` / modeled total stop loss.
  - Preset Manager `Tyres Expected` checkbox is now strictly two-state and preset edit/save normalization writes explicit `TyreStopExpected` intent.

- 2026-05-19: PreRace/StrategyDash formation-fuel adapter parity fix validated.
  - `LalaLaunch.UpdatePreRaceOutputs(...)` now includes planner formation fuel (`FuelCalculator.FormationLapFuelLiters`, clamped non-negative) exactly once in pre-race total fuel need when `SessionState < 3` (before formation starts).
  - This makes `LalaLaunch.PreRace.*` and `StrategyDash.*` pre-start actionable fuel guidance formation-aware (`TotalFuelNeeded`, stints/delta/status, start-fuel required/advice/status, next-refuel target/delta/advice/status, required stops pre-green).
  - Runtime race-running families remain intentionally unchanged and formation-excluded (`Fuel.Refuel.*`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, `Pit.FuelControl.*`) to avoid double-count after formation burn.


- 2026-05-19: Property Snapshot Fuel/Strategy capture follow-up for live pit-tyre prediction seams.
  - Added explicit Fuel/Strategy external snapshot rows for `LalaLaunch.Fuel.Live.TireChangeCount`, `LalaLaunch.Fuel.Live.TireChangeTime_S`, `LalaLaunch.Fuel.Live.TotalStopLoss`, `LalaLaunch.Pit.Box.TargetSec`, `LalaLaunch.Pit.Box.RemainingSec`, and `LalaLaunch.Pit.Box.ElapsedSec`.
  - This is a snapshot observability-only update (no runtime pit/strategy formula changes).

- 2026-05-19: Phase 3B runtime selected-tyre live pit prediction implemented.
  - Added new runtime export `Fuel.Live.TireChangeCount` (`0..4`) from DP per-wheel tyre flags (`dpLFTireChange/dpRFTireChange/dpLRTireChange/dpRRTireChange`) with conservative fail-open fallback to `4` when flags are unavailable or partial.
  - Updated `Fuel.Live.TireChangeTime_S` semantics from all-or-nothing gate to selected-count estimate: `count<=0 => 0`, `count>=4 => full4`, else `shared=1.0 + scaled variable` from learned full-4 tyre time.
  - `Fuel.Live.TotalStopLoss` and boxed target contracts remain unchanged, but now consume the improved live tyre estimate; Strategy `TyresExpected`/preset intent ownership remains planner-only and untouched.

- 2026-05-19 PR #745 review follow-up validated:
  - Preset edit/clone/copy now hydrate/save explicit tyre intent from resolved compatibility state (`ResolvedTyreStopExpected`) instead of nullable raw field.
  - Legacy preset open/save behavior now deterministic in Preset Manager; strategy/live seams unchanged.


- 2026-05-19 Strategy tyre intent separation landed:
  - Presets now own tyre-stop intent (`TyreStopExpected`) rather than tyre timing seconds.
  - Strategy apply/reapply no longer mutates planner tyre slider seconds from preset legacy timing fields.
  - Strategy stop math now uses intent-gated effective tyre time (`OFF=0s`, `ON=slider`).
  - Live pit prediction/export seams remain unchanged (`Fuel.Live.TireChangeTime_S`, `Fuel.Live.TotalStopLoss`).

- 2026-05-18: Property Snapshot rolling recording visual indicator polish validated.
  - Debug Options > Property Snapshot now renders rolling status as a bordered pill beside rolling controls using existing status resolver values (`OFF`/`READY`/`RECORDING`) with distinct emphasis states (grey/blue/green) for immediate in-plugin visibility.
  - Rolling controls now reflect status state in UI: `START` disables while `RECORDING`; `STOP` disables while inactive (`OFF`/`READY`).
  - No rolling logic, status semantics, exports, snapshot capture behavior, DataCore capture, or CSV schema changes.

- 2026-05-18: Property Snapshot rolling status UI refresh coverage follow-up validated.
  - `ROLLING CSV` status label now refreshes immediately when `Enable debugging mode` (Soft Debug) or `Enable Property Snapshot` toggles change, preventing stale OFF/READY/RECORDING display between rolling-control interactions.

- 2026-05-18: Property Snapshot observability follow-up validated.
  - Added debug exports `Debug.PropertySnapshot.RollingStatusText` and `Debug.PropertySnapshot.RollingModeText` for dashboard/plugin-state visibility of rolling capture readiness and active recording state.
  - Property Snapshot capture now also records selected external DataCore fuel properties directly (`DataCorePlugin.Computed.Fuel_*`) under Fuel/Strategy group gating, blank-safe when unavailable.
  - No Fuel.Refuel/Pit.FuelControl/PreRace authority logic changes; update is debug observability only.
- 2026-05-18: Tyre learn corrected persistence jack/drop allowance validated.
  - corrected persisted tyre-service learner now adds fixed `+1.0s` jack/drop allowance to both derived and fixed-tail corrected candidates.
  - derived formula is now `(firstClear-start) + 4*medianInterval + 1.0`; fallback is now `raw + 6.0 + 1.0`.
  - lock behavior, candidate bounds, and all-four-only gating remain unchanged.

- 2026-05-18: Tyre learn Phase 2 corrected persistence validated.
  - clean all-four candidates now persist corrected tyre service-time (derived median-interval preferred, fixed-tail fallback) instead of diagnostic-only raw candidate handling.
  - lock-aware save semantics and runtime/profile tyre-time ownership unchanged; accepted logs now explicitly report corrected vs raw method.

- 2026-05-18: PR #737 review bugfix follow-up (hydration holds correctness).
  - ClassLeader/ClassBest identity hold now applies only when the resolved leader/best `CarIdx` is unchanged; car changes clear identity fields to prevent mismatched car-vs-name exports.
  - CarSA class-rank and driver-info cache holds now clear on session-token change (no cross-session hold leakage); same-session hydration holds remain.
  - ClassBest identity hold logging is now transition-latched (single enter/clear diagnostics, no per-tick spam).

- 2026-05-18: Drivers## metadata hydration hold/readiness follow-up landed.
  - Added Drivers-only hydration hold behavior for ClassLeader/ClassBest identity fields, CarSA iRating/class-est-lap cache, and CarSA class-rank map so short Drivers table gaps no longer clear metadata to blank/0/NaN.
  - `IsCarSaIdentitySourceReady()` now scans Drivers01..Drivers64 for any usable row identity/class metadata instead of relying only on `Drivers01.CarIdx` presence.
  - Added bounded one-time transition diagnostics for hold/recovery states; no `CompetingDrivers` fallback restored and no denominator ownership/count paths changed.
- 2026-05-18: Tyre learning correction instrumentation pass validated (diagnostic-only, save-path unchanged).
  - Added one compact `[LalaPlugin:Tyre Learn] sample ...` line per clean all-four candidate, reporting raw all-four-clear timing, fixed `+6.0s` correction, derived-tail `+1.0s` correction (when derivable), per-wheel clear offsets, pit service status/flags, stall-exit sample, and pit-stop elapsed sample.
  - Follow-up diagnostics now also report wheel clear order, per-wheel clear timestamps, interval statistics (`d1/d2/d3`, avg, median), per-tyre estimate, corrected 4-tyre estimate, retained saved tyre time, and pit entry/exit timestamps for future per-tyre model validation.
  - Follow-up ordering fix now records per-wheel `1->0` transitions before `allFourCleared` sample emission in `ServiceStarted`, so final-wheel clear tick samples include all four wheel timestamps/order and populated interval metrics.
  - Follow-up tidy now prints `NA` for missing wheel offsets in the sample line.
  - Follow-up diagnostic context fix now clears stale `pitExit` on each new `pitEntry` edge, and `savedNow` now reports direct runtime/profile stored tyre time (not tyre-selection-gated), so all-four-clear samples no longer show stale previous-stop exit or `savedNow=0` artifacts.
  - PR #734 safety follow-up now disables raw learner persistence from the diagnostic path: clean all-four raw candidates still produce sample diagnostics but no longer call profile save/update runtime tyre time; diagnostic path emits `diagnostic-only: raw candidate not persisted.`.
  - `savedNow` in diagnostics now prefers persisted profile value first, then runtime fallback, with invalid values sanitized to `0.0`.
  - Added per-wheel clear timestamp capture (`LF/RF/LR/RR`) plus first/last clear tracking inside tyre learner runtime state.
  - Candidate detection/state-machine shape and reject-path semantics remain unchanged; only diagnostic context/safety behavior changed.
- 2026-05-18: PR #736 review follow-up hardened Property Snapshot PER LAP write path with local exception guard.
  - wrapped `MaybeWritePropertySnapshotPerLap(...)` rolling write in `try/catch` and downgraded write failures to bounded warning logging, preserving lap-tick processing continuity and existing manual/frequency error-handling behavior.


- 2026-05-18: Property Snapshot rolling automation hardening validated (PR #736 follow-up).
  - automation active state no longer persists across restart/reload (runtime-only + persisted flag clear-on-init).
  - START now refuses to arm unless Soft Debug + Property Snapshot + Rolling CSV toggles are all enabled.
  - FREQUENCY mode cap reduced to 2 Hz due to full-file rolling wide rewrite cost; auto logging throttled to prevent debug log spam.

- 2026-05-18: Property Snapshot rolling automation modes + controls validated (Part 2).
  - Debug Options > Property Snapshot now includes rolling mode selector (`MANUAL`/`FREQUENCY`/`PER LAP`), frequency setting, and explicit `START`/`STOP`/`RESET ROLLING CSV` controls.
  - Manual Event Marker semantics preserved; manual captures still write one-shot snapshot files and optional rolling column append.
  - Automatic rolling capture is gated by START + rolling-enabled toggle: FREQUENCY uses guarded 1..5 Hz cadence (default 1), PER LAP reuses existing lap-cross seam and prevents duplicate same-lap capture.
  - Rolling reset clears only `PropertySnapshot_Rolling.csv` (primary + fallback), without touching one-shot files, group settings, or snapshot include-changed behavior.

- 2026-05-18 final tidy: aligned active docs/contracts with Drivers-only identity and renamed Drivers-row counter helpers for clarity.
  - Active ClassLeader/ClassBest docs now describe `DriverInfo.Drivers##` identity seams only (no CompetingDrivers fallback contract).
  - Renamed denominator support helpers to `CountValidDriversRowsExcludingPaceCar` / `CountPlayerClassDriversRowsExcludingPaceCar` to match current `Drivers##` data source usage.

- 2026-05-18 PR follow-up: removed remaining `DriverInfo.CompetingDrivers` runtime reliance and hardened native class-match identity for denominator fallback.
  - Race and League denominator support paths now consume `DriverInfo.Drivers##` only (including roster count, CarSA class-rank map source, identity and driver-info resolution helpers, and class-short resolution fallback).
  - `GetNativePlayerClassDriverCount()` now matches by native class ID (`PlayerCarClassID`/`DriverCarClassID` + row `CarClassID`) with class-name fallback, reducing short-name/id mismatch zero-count risk.

- 2026-05-18: League race class denominator source corrected to strict `DriverInfo.Drivers##` subclass cohort for League ON.
  - `GetLeagueClassPlayerDriverCount()` and `ResolveCanonicalPlayerClassRaceDenominator()` support paths now use strict current-session `DriverInfo.Drivers##` row scanning/resolution for League subclass counts (pace-car excluded), with no CSV membership fallback as race denominator authority.
  - `Race.PlayerClassFieldSize` / `RaceFinish.PlayerClassFieldSize` canonical seam now uses strict League subclass count when available, else native/session fallback only.

- 2026-05-17: Live race class denominator publish path tightened.
  - `Race.PlayerClassFieldSize` now attaches directly to `ResolveCanonicalPlayerClassRaceDenominator(...)`; no League-specific live override path remains. `RaceFinish.PlayerClassFieldSize` path unchanged.

- 2026-05-18: Property Snapshot grouping audit + contract guardrail validated.
  - audited Property Snapshot group mapping against current plugin export surface (`AttachCore`/`AttachVerbose`) and internal inventory/changelog references.
  - expanded grouping coverage so `Race.*`, `RaceFinish.*`, `ClassBest.*`, and `ClassLeader.*` capture under `Car/Opp/H2H` instead of defaulting to `Raw Debug`.
  - grouped `Pace.*` and `Surface.*` under `Fuel/Strategy` for planning/fuel-context snapshot visibility.
  - no export/property names changed and no runtime subsystem logic changed.
  - `Docs/Internal/CODEX_CONTRACT.txt` now includes a standing mandatory rule: any task that adds/removes/renames/behavior-changes SimHub exports/properties must review/update Property Snapshot grouping and explicitly report `Property Snapshot list reviewed: yes/no, with reason.`

- 2026-05-17: Strategy Planner profile preview stale-label regression fix validated.
  - Profile-mode AVG/ECO/MAX preview row now clears immediately on missing/deleted track fuel context (no Live Snapshot toggle required).
  - fix is UI-refresh scoped (`PropertyChanged` on profile preview display fields during track-scoped reset); no runtime DATA authority, pit command behavior, or refuel-formula behavior changes.

- 2026-05-17: PR #730 follow-up fixed tyre-time profile copy parity.
  - profile clone/copy paths now copy `TireChangeTime` whenever `TireChangeTimeLocked` is copied, preventing locked-with-stale-value destination states.
  - removed unused `_tyreLearnLastAllFourSelected` field from runtime learner state storage (no learning-semantics change).

- 2026-05-17: Tyre change time learning + lock model landed (car-level, conservative all-four learning).
  - Added profile field `TireChangeTimeLocked` (default false for legacy profiles) and propagated default/clone/copy paths.
  - Added Profiles CAR-tab tyre timing lock UI beside tyre-change time control.
  - Added lock-aware tyre-time persistence seam mirroring refuel behavior (`locked suppress`, `locked first-fill when no usable stored value`).
  - Added bounded `[LalaPlugin:Tyre Learn]` state-machine logging and learning path using only per-wheel tyre flags (`dpLFTireChange/dpRFTireChange/dpLRTireChange/dpRRTireChange`), rejecting uncertain candidates conservatively.

- 2026-05-16: Race player-class denominator authority fix landed.
  - `Race.PlayerClassFieldSize` now uses a canonical current-session denominator path and no longer takes League CSV registered class-membership fallback through `LeagueClass.Player.DriverCount`.
  - League-enabled denominator remains current-session effective cohort first, with native/session telemetry class-denominator fallback when cohort data is unavailable.
  - `RaceFinish.PlayerClassFieldSize` class-snapshot freeze now uses the same canonical denominator helper and can refresh while class snapshot is active/player snapshot pending only when initial value was invalid (`0`).

- 2026-05-16: RaceFinish player finish retry-gap baseline latch validated: first observed player-finish session timestamp is latched before class-position retry guard, so delayed snapshot capture does not drift `RaceFinish.PlayerFinishGapSec`.

- 2026-05-16: RaceFinish deferred-reset apply-path hardening validated: pending identity is applied only if it still matches current observed session identity; stale pending is discarded and active defer-log latch resets on churn reversion.

- 2026-05-16: PitExit League cohort authority follow-up validated.
  - when League Class is enabled and live player effective class resolves valid, PitExit/Opp race-context cohort matching now stays on effective-class authority (candidate rows must resolve valid effective class to match), preventing native-equivalent cohort carry-over with colour-only changes.
  - when League Class is enabled but live player effective class is unresolved, native fallback remains guarded by valid native class colours on both rows (no match-all).
  - added narrow League settings/definition-change invalidation path to refresh Opponents/PitExit cohort state on next tick.
  - protected systems unchanged: PitExit timing/gap/countdown/loss/distance math, Opp race-slot ordering/gap math, CarSA, H2H, and fuel systems.

- 2026-05-15: RaceFinish deferred identity stale-clear validated: transient `A→B→A` churn during active finish lifecycle now clears pending deferred identity, avoiding extra reset/fuel-recalc cycle from stale pending apply.

- 2026-05-15: RaceFinish deferred-session detectability follow-up validated: deferred path now stores pending session identity without overwriting active finish-session identity, then applies one clean reset/identity update after lifecycle exit.


- 2026-05-15: PR #722 review follow-up fixed unresolved-player native fallback guard.
  - unresolved-player path in League race-context matcher now compares native class colors only when both sides have valid non-empty class color values; missing colors now fail closed (`false`) instead of matching cohort-wide.
  - race-context native row builder now populates `NativeCarRow.ClassColor` so native fallback comparison remains available when class color data exists.
  - protected systems unchanged: League enabled+valid behavior, disabled behavior, PitExit timing/gap/countdown/loss/distance math, Opp race-slot math, CarSA, H2H.

- 2026-05-15: PitExit League-class cohort selection reliability fix landed.
  - fixed race-context class matcher delegate gating so League-enabled sessions always provide a matcher delegate; player effective-class resolution now runs from the active race-model player row identity at match time.
  - removes native-only fallback caused by transient preview-player unresolved state; PitExit classRows and selected ahead/behind targets now use effective-class cohort when enabled+resolved.
  - native fallback behavior is preserved when League is disabled or player effective class is unresolved at match time.
  - no change to PitExit timing/gap/countdown/loss/distance math; no change to Opp race slots, CarSA, or H2H ownership paths.

- 2026-05-14: StrategyDash start-fuel setup-fallback phase-gate fix landed.
  - `StrategyDash.StartFuelAdviceText` / `StrategyDash.StartFuelStatus` now use setup fallback only when pre-race/grid/formation fallback is allowed (`SessionState < 4`), while keeping live-fuel-first precedence and unknown fallback behavior unchanged.
  - active race-running (`SessionState == 4`) no longer allows setup fuel to influence StrategyDash start-fuel advice/status.

- 2026-05-14: Fuel dash support export/cap alignment landed. Added `Fuel.Live.RemainingStints`, `Fuel.MaxTank`, `Fuel.PitStopsRequiredByFuelExact`, `Pit.FuelControl.TargetText`, and `Fuel.Refuel.SelectedBurnPerLap`; capped `Pit.FuelControl.TargetLitres` by runtime max-tank authority while preserving numeric contract; `Pit.LastPaceDeltaNetLoss` lifecycle restored to pre-#718 behavior (no pit-entry clear).
- 2026-05-14: Planner/Pit lifecycle follow-up landed.
  - Strategy Planner live snapshot fuel-per-lap now clears live fuel cache on new live car/track combo and falls back to profile condition fuel/source when current-session live fuel authority is unavailable.
  - PR review follow-up: when live fuel authority returns and value is inside deadband, planner now still updates `FuelPerLapSourceInfo` back to live so source-aware UI does not remain on profile fallback labels.
  - `Pit.Box.LastDeltaSec` now persists post-stop for review, clears on next boxed stop start, and clears on reset/session reset (no short auto-expire window).

- 2026-05-13 Pit Fuel Control legacy PushSave surface removal landed:
  - removed legacy action `Pit.FuelControl.PushSaveModeCycle` and legacy compatibility exports `Pit.FuelControl.PushSaveMode` / `Pit.FuelControl.PushSaveModeText`;
  - removed legacy PushSave compatibility settings/property wiring and moved UI binding to canonical DATA setting state (`PitFuelControlDataMode` / `PitFuelControlDataPlanModeEnabled`);
  - kept DATA SAVED PUSH/SAVE guard behavior unchanged and renamed UI wording to `DATA SAVED Push/Save Guard (%)`.

- 2026-05-13 after-stop delta DATA-governance update landed:
  - existing after-stop exports `Fuel.Delta.LitresPlan`, `Fuel.Delta.LitresPlanPush`, `Fuel.Delta.LitresPlanSave` now keep name/meaning (after planned add) but select burn/lap basis from Pit Fuel Control DATA (`LIVE` live/stable, `SAVED` planner/profile only);
  - added plugin-owned selected export `Fuel.Delta.AfterStop.Selected` with SOURCE selection (`PUSH/SAVE/NORM/STBY=>PlanPush/PlanSave/Plan/Plan`, STBY advisory NORM);
  - no pit command send behavior changes and no changes to `Fuel.Delta.LitresCurrent*` families.
  - PR review follow-up: DATA SAVED after-stop basis now blocks runtime fallback fuel authority and invalid/no-basis paths clear `Fuel.Delta.AfterStop.Selected` with `Fuel.Delta.LitresPlan*` to avoid stale non-zero carry-over.
  - late review fix: DATA-governed resolver now guards null `GameData/NewData` so pre-first-tick PLAN snapshot paths fail-safe instead of throwing.
- 2026-05-13 Pit Fuel Control NORM DATA-basis alignment landed:
  - `BuildPitFuelControlSnapshot()` now applies DATA authority to `NORM` target selection in addition to existing `PUSH/SAVE` behavior;
  - `DATA LIVE + NORM` keeps current runtime/live stable normal targeting;
  - `DATA SAVED + NORM` now uses planner/profile normal-basis targeting and does not take LIVE/SIM authority on this path;
  - no changes to PUSH/SAVE behavior, DATA toggle/reset behavior, source/mode cycling, AUTO arming, or pit command send semantics.
  - review follow-up fixed the `TryResolvePlanNormNeed(...)` call-site parameter names to the current `ResolveDataGovernedBurnAndPaceBasis(...)` signature.
  - review follow-up removed stale Dash Integration wording that contradicted NORM DATA-basis behavior.
  - review follow-up now threads live `GameData` into `TryResolvePlanNormNeed(...)` and through to `ResolveDataGovernedBurnAndPaceBasis(...)`, removing the null `data` handoff that could throw on PLAN+NORM paths.

- 2026-05-13 PitExit League Class identity follow-up landed:
  - fixed PitExit class-color presentation resolver identity by passing selected ahead/behind target `UserID` into League race-context color resolution, enabling CSV-only color mapping on PitExit exports;
  - format contract preserved (`PitExit.Ahead/Behind.ClassColor` remains `0xRRGGBB`), with unchanged native fallback and unchanged PitExit selection/math domains.

- 2026-05-13 League Class authority alignment follow-up landed:
  - `Car.Player.PositionInClass` now uses the existing effective-class position seam when League Class is enabled+resolved, with native fallback unchanged when disabled/unresolved;
  - `PitExit.Ahead.ClassColor` / `PitExit.Behind.ClassColor` now publish through the existing League race-context presentation seam while preserving `0xRRGGBB` format and native fallback behavior;
  - PitExit selection and prediction math domains remain unchanged (`PredictedPositionInClass`, gaps, countdown, loss, distance semantics preserved).

- 2026-05-11 RaceFinish class-field-size source-order follow-up landed:
  - class denominator resolution now prefers effective class driver-count seam, then native telemetry `OpponentsInClassCount + 1`, then SimHub baseline `GameData.NewData.OpponentsInClassCount + 1`;
  - preserves valid solo-class denominator (`0` opponents => `1`) and leaves finish timing/snapshot triggers unchanged.

- 2026-05-11 fuel burn authority-chain fix landed:
  - in no-accepted-lap runtime path, `LiveFuelPerLap` now uses active-condition profile fuel when valid and only uses SimHub/DataCore fallback as last resort;
  - added bounded `[LalaPlugin:Fuel Burn] runtime burn basis selected ...` transition log for source/value auditability.
  - follow-up narrowed that log throttle signature to true authority state (`source`) so fallback-input jitter cannot spam repeated transition logs.

- 2026-05-11 rolling snapshot legacy schema guard fix:
  - rolling `PropertySnapshot_Rolling.csv` reuse now validates header column 0 as exact `SimHubProperty` before parsing existing rows;
  - legacy header `SnapshotUtc` and malformed/unknown headers are treated as incompatible and safely reset/re-written in current wide schema;
  - added bounded debug info log when a rolling schema reset occurs.

- 2026-05-11 property snapshot final polish pass:
  - UI cleanup: `Select All` now drives all Property Snapshot group checkboxes, and individual group toggles now back-sync `Select All`;
  - rolling hardening: snapshot value text now normalizes CR/LF to literal `\n` before CSV write to keep wide rolling reload line-safe.

- 2026-05-10 rolling CSV parser correctness fix:
  - rolling wide-file reload now parses CSV with quote-aware logic (no naive `Split(',')`), preventing comma/quote-containing values from corrupting column alignment over subsequent captures.

- 2026-05-10 rolling snapshot layout + fallback path bugfix:
  - rolling `PropertySnapshot_Rolling.csv` now writes in wide format (`SimHubProperty` rows, one new timestamp column per capture) for easier left-to-right comparison;
  - one-shot fallback writes now preserve relative subfolder paths (including `PropertySnapshots`) instead of flattening to fallback root.

- 2026-05-10 property snapshot changed-values observability tweak:
  - added per-capture summary log with `includeChanged`, row count, and `changed1/changed0/changedNA` totals to make changed-comparison behavior explicit during testing.

- 2026-05-10 property snapshot one-shot file organization tweak:
  - one-shot `PropertySnapshot_<...>.csv` files now write under `.../Logs/LalaPluginData/PropertySnapshots/` for cleaner folder organization;
  - rolling `PropertySnapshot_Rolling.csv` remains at `.../Logs/LalaPluginData/`.

- 2026-05-09 property snapshot path parity + gating diagnostics fix:
  - aligned snapshot primary CSV path with existing plugin CSV writers (`<SimHub install>/Logs/LalaPluginData`) and kept Documents fallback on write/append failure;
  - added one-time warning when Property Snapshot is enabled while Soft Debug is off, plus one-time info log of resolved primary/fallback snapshot paths.

- 2026-05-09 property snapshot per-press failure handling fix:
  - failed marker snapshot attempts now stamp the processed press count in the failure path to prevent repeated retries/log spam from a single press; next snapshots still require a new marker press.

- 2026-05-09 property snapshot write-path hardening follow-up:
  - snapshot one-shot and rolling writes now retry to Documents fallback when Program Files primary path fails during file write/append (including ACL-denied existing-folder cases);
  - snapshot export no longer permanently disables after a single primary IO failure;
  - added action-bounded diagnostics for marker press registration, snapshot write success, rolling append success, and fallback-path usage.

- 2026-05-09 finish-reference-retry follow-up landed:
  - when SS5 transition tick lacks valid overall-leader lap-pct sample, finish-reference capture now retries on later `SessionState>=5` ticks until captured;
  - once captured, the reference remains latched until finish-timing reset.
- 2026-05-09 post-lifecycle sample anchoring follow-up landed:
  - class-leader dynamic-reference crossing/wrap fallback now requires prior sample captured after lifecycle start (`SessionState>=5`);
  - pre-transition sample evidence remains scoped to guarded already-crossed-on-transition path only.
- 2026-05-09 pre-merge finish follow-up landed:
  - solo-class denominator behavior restored (`OpponentsInClassCount=0` now correctly freezes class field size as `1`);
  - dynamic finish-reference class-cross latch now requires true prior->current crossing evidence (wrap/already-crossed guards retained).
- 2026-05-09 finish-semantics correction landed:
  - SessionState `4->5` is now documented/treated as overall race lifecycle/overall-leader finish phase (not unconditional class-leader finish in multiclass);
  - class-leader finish remains independently resolved; multiclass lifecycle fallback now uses dynamic finish-reference pct capture at `SessionState 4->5` plus class-leader own crossing/wrap evidence (no class-vs-overall lap-count comparison);
  - RaceFinish class/player split-stage behavior preserved with `SessionState==6` as safety fallback only.
- 2026-05-09 RaceFinish replay fallback + class field-size freeze reliability follow-up landed:
  - player snapshot replay reliability now uses robust player-checkered seams (`GameData.Flag_Checkered` / `SessionFlagsDetails.IsCheckered*`) alongside per-car finish-like bits;
  - fallback ordering preserved (`flags/checkered -> SessionState==6` safety only);
  - class field-size freeze avoids zero-opponent lock-in and now reuses effective class cohort fallback when class telemetry count is unavailable/zero.
- 2026-05-09 snapshot path alignment fix for Property Snapshot:
  - snapshot files now target `Program Files (x86)/SimHub/Logs/LalaPluginData` with Documents fallback when Program Files x86 path is unavailable.

- 2026-05-09 stale disabled-window marker presses fix for Property Snapshot:
  - while snapshot mode is disabled, marker press counter is now consumed so old presses do not trigger captures on later re-enable in the same session.

- 2026-05-09 per-press trigger + Car.Debug bucket fix for Property Snapshot:
  - `Car.Debug.*` now routes to `RawDebug` classification bucket;
  - snapshot trigger now processes each Event Marker action press (counter-based), including repeated presses during the event pulse window.

- 2026-05-09 missing sanitizer helper fix for Property Snapshot:
  - added concrete CSV sanitize/value-string helpers used by snapshot writer to restore compile validity and safe CSV quoting.

- 2026-05-09 follow-up review bugfixes for Property Snapshot:
  - guarded snapshot file writes against IO exceptions with one-time self-disable + warning;
  - stabilized rolling CSV schema by always keeping `ChangedVsPrevious` column present (`NA` when compare mode is off).

- 2026-05-09 debug snapshot system landed:
  - added Event Marker-triggered `PropertySnapshot_<UTC>_Sess<SessionTimeSec>.csv` debug export (group-filtered property snapshot with `SimHubProperty/InternalSource/Value/GroupType`);
  - optional `ChangedVsPrevious` column compares against prior snapshot (`NA` when no baseline);
  - optional rolling append output `PropertySnapshot_Rolling.csv`;
  - added core visibility export `Debug.PropertySnapshotEnabled`.
- 2026-05-09 RaceFinish live-then-freeze follow-up landed:
  - player-facing RaceFinish fields (`PlayerOverallPosition`, `PlayerClassPosition`, `PlayerFuelLeft`, `PlayerBestLap*`) now remain neutral before class snapshot, publish live values while class snapshot is active and player snapshot pending, then freeze at player snapshot;
  - `RaceFinish.PlayerOverallFieldSize` and `RaceFinish.PlayerClassFieldSize` now freeze at class snapshot for stable `Pxx / yy` dash denominators through player finish/session end;
  - Added live plugin-owned `Race.FieldSize` and `Race.PlayerClassFieldSize` exports; counts are roster-first and explicitly exclude pace-car rows before fallback sources.
  - overall position fallback was tightened to preserve strict overall-rank semantics only (no class-position fallback when overall source is unavailable);
  - `RaceFinish.PlayerFinishGapSec` remains canonical gap timer and `ClassWinnerGapSec` remains compatibility mirror.

- 2026-05-08 docs sweep landed:
  - added canonical subsystem documentation `Docs/Subsystems/League_Class_System.md` covering resolver precedence, fallback hierarchy, UI workflow, export contract, and ownership boundaries;
  - aligned subsystem/internal cross-references (`Project_Index`, `H2H`, `Dash_Integration`, `SimHubParameterInventory`, `Plugin_UI_Tooltips`) to point to the canonical League Class doc and reduce duplication/stale drift.

- 2026-05-07 League Class final polish landed:
  - added Dash Control `Bindings` row `League Class Toggle` wired to existing plugin action `LalaLaunch.LeagueClass.ToggleEnabled` (no duplicate action added);
  - League-aware dash class-name presentation now uses effective League `ShortName` when available and falls back to effective League `Name` only when `ShortName` is blank;
  - disabled/unresolved League behavior remains native fallback; no changes to class color, effective position, CarSA/Opponents/H2H selection-ordering logic, or manual-override scope.

- 2026-05-07 Fuel.Refuel DATA projection follow-up landed:
  - removed the incorrect `simLapsRemaining > 0` reprojection gate so DATA SAVED timed-race paths can remain valid when time context + selected lap-seconds are sufficient;
  - contingency configured in laps is now converted using the selected Fuel.Refuel burn basis (`selectedBurn`) used by `Fuel.Refuel.NextLitres` (including PUSH/SAVE and STBY advisory NORM), not stable/NORM-only contingency litres.

- 2026-05-07 Fuel.Refuel DATA-governed projected-laps alignment landed:
  - runtime `Fuel.Refuel.*` guidance now resolves projected remaining laps from the same selected DATA-governed lap basis exported by `Fuel.Refuel.LapSource` (instead of always using live stable laps);
  - DATA LIVE keeps LIVE-first usage when `LapSource=LIVE` with valid live projection, while non-live lap sources (`PLAN/PROFILE/DEFAULT`) now reproject from selected lap seconds using existing race projection semantics;
  - unresolved DATA-governed projected-laps context now deterministically invalidates runtime refuel guidance (`Fuel.Refuel.Valid=false`, `Fuel.Refuel.NextText=CHECK FUEL`).

- 2026-05-07 Fuel.Refuel review follow-up landed:
  - invalid Fuel.Refuel reset paths now clear stale source/basis context (`BurnSource/LapSource=DEFAULT`) and refresh `DataMode/BurnMode` from current Pit Fuel Control state (safe fallback `LIVE`/`STBY`);
  - DATA SAVED + PUSH/SAVE fallback source labelling now reflects actual derivation from selected normal basis (`SAVED`/`PROFILE`/`LIVE`) instead of always reporting `DEFAULT`.

- 2026-05-07 Fuel.Refuel NextStopCap follow-up landed:
  - final-stop vs multi-stop decision threshold now uses runtime effective restricted tank-cap authority seam (`ResolveRuntimeLiveMaxTankCapacity`) instead of current on-track tank-space;
  - multi-stop displayed litres remain add-guidance from runtime add-cap seam (`Fuel.Pit.TankSpaceAvailable`) to avoid overstating a full-tank-size add amount while still indicating max-fill intent;
  - `Fuel.Refuel.Valid` now fails when capacity context is unusable, including zero decision capacity with positive requirement.

- 2026-05-07 Fuel.Refuel runtime tactical export family landed:
  - added `Fuel.Refuel.*` runtime exports (`NextLitres`, `NextLitresCeil`, `NextText`, `Valid`, `BurnSource`, `LapSource`, `DataMode`, `BurnMode`) as canonical race-running next-stop guidance surface;
  - `NextLitres` now uses final-stop vs multi-stop threshold semantics (`FinalStopNeed` vs usable next-stop add capacity) with contingency included once on final-stop guidance and not repeatedly stacked on non-final max-fill guidance;
  - STBY now publishes truthful mode context while computing advisory NORM guidance when runtime math is valid (no command/send implication);
  - command transport/send behavior and protected runtime domains (`Fuel.Delta.*`, `Fuel.Pit.*`, `Fuel.RequiredBurnToEnd*`, boxed refuel latches, StrategyDash next-refuel helpers) remain unchanged.
- 2026-05-07 PreRace one-stop under-fuel severity review landed:
  - `2 STINT PLAN REQUIRES MORE FUEL` now publishes `orange` (was `red`) when one-stop is still feasible but under target;
  - intent is high-priority advisory parity with next-stint review, while hard invalidity remains `2 STINT PLAN NOT POSSIBLE` (red).

- 2026-05-06: PR #687 follow-up fixed a compile blocker in `LalaLaunch` by removing invalid nullable coalescing from CarSA Ahead/Behind `PositionInClass` publish lambdas; effective-position seam behavior unchanged.
- 2026-05-06 CarSA PositionInClass publish-seam alignment landed:
  - `Car.Ahead01..05.PositionInClass` / `Car.Behind01..05.PositionInClass` now publish through the shared effective-position seam (`GetEffectivePositionInClassForPublishedContext`), using Opponents effective race-context rank when available and native slot fallback otherwise;
  - no CarSA selection/order/filter/cache changes and no Opponents/H2H/PitExit logic changes.

- 2026-05-06 PreRace status catalogue/phase update landed:
  - `LalaLaunch.PreRace.StatusText` now uses approved stint-focused catalogue wording (`ADD START FUEL FOR SINGLE STINT`, `SINGLE STINT OKAY/POSSIBLE`, `2 STINT PLAN ...`, `MULTI STINTS REQUIRED`, etc.);
  - max-fuel warning split is active: `MAX START FUEL REQUIRED` (start fuel) and phase-routed `SET MAX FUEL NEXT STINT` in StrategyDash `START READY` / `RACE`;
  - StrategyDash phase contract is now `0=IDLE`, `1=PRE GRID`, `2=GRIDDING`, `3=START READY`, `5=RACE`;
  - added `Docs/Internal/FuelSystemMessages_Catalog.csv` as PR-safe text authority for the catalogue/split update; binary spreadsheet sync is deferred to manual/local conversion.

- 2026-05-06 DATA LIVE provenance follow-up landed: PreRace DATA basis labels now emit true selected provenance tokens (`LIVE|PLAN|PROFILE|SIM|DEFAULT`) with DATA SAVED never emitting LIVE/SIM; retired `StrategyDash.IsAutoStrategy` export/wiring.

  - PR #684 P1 follow-up: stable projection source `fuelcalc.estimated` now maps to LAP `SAVED` in the DATA resolver (prevents temporary `DEFAULT` regression when planner estimate is the held stable lap).

- 2026-05-06 H2H ClassColorHex coverage follow-up landed:
  - added H2H target `ClassColorHex` exports (`H2HRace.Ahead/Behind`, `H2HTrack.Ahead/Behind`) and aligned them to the existing League-aware presentation gate with `#RRGGBB` format.

- 2026-05-06 League Class follow-up landed:
  - `Opp.Ahead/Behind*.ClassColor` keeps canonical `0xRRGGBB` format under League-aware presentation; `ClassColorHex` remains `#RRGGBB`;
  - DriverCount player-car identity reads now use CarIdx invalid sentinel `-1` to avoid missing-value car-0 misclassification;
  - authorised H2HTrack class-color presentation now follows League-aware path via shared H2H attach wiring only (no selector/sector/delta/gap/timing changes).

- 2026-05-06 League Class follow-up fix landed:
  - `LeagueClass.Player.DriverCount` now includes player row under manual override/effective-player-class semantics while counting non-player rows via driver resolver semantics;
  - League class-color presentation override in shared H2H export attach path is now restricted to H2HRace; H2HTrack class color remains native.

- 2026-05-06 League Class presentation follow-up review fixes landed:
  - `LeagueClass.Player.DriverCount` now counts drivers in the selected player effective class cohort (not total CSV-valid rows), with native fallback or `0` when unavailable;
  - H2HRace class presentation resolution now passes participant `UserID` when available so CSV-only mode resolves class presentation correctly.


- 2026-05-06 League Class race-context class presentation alignment landed:
  - Opp race-context slots (`Opp.Ahead1..5` / `Opp.Behind1..5`) now publish class presentation fields from effective League Class while preserving original slot identity and native fallback when League Class is disabled/unresolved;
  - H2HRace player/ahead/behind class presentation follows the same League Class gate for class-facing dash fields only (no H2HTrack or sector/delta changes);
  - added dash export `LeagueClass.Player.DriverCount` for selected player effective-class cohort count display intent.
- 2026-05-06 PR #679 build-fix landed:
  - restored missing `ResolveDataGovernedBurnAndPaceBasis(...)` helper in `LalaLaunch` (PreRace helper region) so `UpdatePreRaceOutputs(...)` compile path is valid again;
  - hierarchy/source contracts preserved exactly:
    - DATA LIVE burn: `LIVE -> PLAN -> PROFILE -> DEFAULT`; lap: `LIVE -> PLAN -> PROFILE -> SIM -> DEFAULT`;
    - DATA SAVED burn: `PLAN -> PROFILE -> DEFAULT`; lap: `PLAN -> PROFILE -> DEFAULT`;
  - protected runtime domains untouched (`Fuel.Delta.*`, `Fuel.Pit.*`, `Fuel.RequiredBurnToEnd*`, boxed refuel latches, `PitFuelControlEngine` target/send behavior).

- 2026-05-05 Pit Fuel Control DATA/SOURCE simplification landed:
  - retired `SOURCE=SAVED`; source cycle is now `STBY -> NORM -> PUSH -> SAVE -> STBY`;
  - added `Pit.FuelControl.Data` / `DataText` and actions `SetDataLive`, `SetDataPlan`, `CycleData`;
  - DATA defaults to `LIVE` on control/session reset, and changing DATA forces `SOURCE=STBY` with no fuel command send;
  - legacy `SetPlan` remains for one release as `DATA=SAVED` + `SOURCE=STBY` feedback-only compatibility (`FUEL DATA SAVED`);
  - StrategyDash next-refuel target and burn-plan text now follow the DATA/SOURCE model (`NORM` runtime/live; `PUSH`/`SAVE` live or planner/profile memory by DATA).

- 2026-05-05 StrategyDash burn-basis alignment + no-stop burn-plan + refuel-delta landed:
  - Auto PreRace fuel-per-lap fallback order now stays live-stable first, then selected planner `FuelCalculator.FuelPerLap` when valid, then existing profile/generic fallback;
  - added `StrategyDash.BurnPlanText` as a concise pre-green helper for no-stop/grid guidance (`BURN PLAN: NORM/SAVE/PUSH` with optional source suffix);
  - added `StrategyDash.NextRefuelDeltaLitres` (`requested - target`) and aligned `StrategyDash.NextRefuelStatus` to `abs(delta)` thresholds (`<=0.5 OK`, `<=2.0 CHECK`, else ACTION) across one-stop/multi-stop paths.

- 2026-05-05 League Class replay-identity follow-up landed:
  - fixed ClassLeader/ClassBest race-context cohort gating to avoid native fallback when League matcher is active but native class-short is unresolved in multiclass replay states;
  - removed synthetic `car:{idx}` race-context identity fallback in `LalaLaunch` class leader/class best candidate rows so matching now relies on canonical Opponents-style identity (`ClassColor:CarNumber`) only;
  - PitExit remains on shared Opponents race-context matcher seam; no pit-exit math/countdown changes.

## Documentation sync status
- 2026-05-07 pre-merge cleanup landed for League Race UI polish:
  - duplicate CSV rows alone no longer trigger yellow helper warning state;
  - player preview resolution now uses a shared helper (single resolver call path per property evaluation);
  - enable-toggle quiet self-check no longer performs redundant double reload.

- 2026-05-07 League Race settings UI polish + toggle-action self-check landed:
  - CSV status helper is now compact (`Status | Rows | Valid | Invalid | Duplicates`) with full count preservation.
  - League Race helper/status text now switches to yellow only for warning/problem states (CSV path/file issues, load/no-valid-row issues, invalid/duplicate row counts, unresolved/invalid player effective class while enabled).
  - Player race class row now uses mode-aware presentation: Auto-detect read-only resolved preview vs Manual override editable fields.
  - `LeagueClass.ToggleEnabled` now performs quiet CSV self-check reload on enable in CSV-capable modes when file exists but valid rows are not loaded; missing/empty path does not block enable.

- 2026-05-05 League Class ClassLeader native-gate bypass fix landed:
  - fixed `FindResolvedClassLeaderCarIdx(...)` so active League race-context matching (League Class enabled + valid player effective class, including manual player override) bypasses native single-class and native player class-short gates before selecting the leader;
  - ClassLeader now chooses the lowest positive `CarIdxPosition` inside the effective-class cohort and self-matches the player car directly, so a player leading the forced/resolved League class publishes `ClassLeader.CarIdx == playerCarIdx`;
  - disabled League Class and enabled-but-unresolved-player paths retain native fallback; no Opponents slot selection, H2HRace/H2HTrack sector/delta, CarSA, or PitExit timing/gap/countdown changes.

- 2026-05-05 League Race final cohort integration follow-up landed (PR #669 replay fix):
  - fixed ClassLeader/ClassBest race-context candidate matching to build native race-context rows from the same session identity sources used by Opponents (`UserID` + `UserName`/`CarNumber`/`CarClassColor` identity key), instead of synthetic `car:{idx}` rows with blank names;
  - this restores League Class cohort matching when resolver paths depend on name fallback (e.g., suffix/manual flows where `UserID` may be absent) while preserving native class fallback when League Class is disabled/unresolved;
  - PitExit cohort path remains delegated via `BuildRaceContextLeagueClassMatchDelegate()` into `Opponents.Update(...)` unchanged; no pit-exit timing/gap math changes.

- 2026-05-05 StrategyDash start-fuel advice decoupling follow-up landed (PR #660):
  - `StrategyDash.StartFuelAdviceText/StartFuelStatus` now come from a dedicated start-fuel comparison (live fuel first, setup fallback second, unknown otherwise) against `StrategyDash.StartFuelRequiredLitres` with `1.0 L` tolerance;
  - start-fuel text/status no longer map from `LalaLaunch.PreRace.StatusText/StatusColour`;
  - preserved invariants: `PreRace.StatusText` and `StrategyDash.NextRefuelAdviceText` unchanged; no changes to Fuel DATA/MODE/SOURCE, `PitFuelControlEngine`, `Fuel.Delta.*`, `Fuel.Pit.*`, or `Fuel.RequiredBurnToEnd*`.

- 2026-05-05 Build-fix follow-up landed:
  - fixed nullable pit-loss read in `FuelCalcs.SavePlannerDataToProfile()` (`PitLaneLossSeconds ?? 0.0`) to resolve CS0266;
  - fixed race-context `TryGetCarDriverInfo(...)` call-site argument types in `LalaLaunch.IsRaceContextClassMatchForCarIdx(...)` (arg 11 stays `out int teamId`) to resolve CS1503;
  - no intended runtime behavior change (compile restoration only).

- 2026-05-04 StrategyDash IsOnTrackCar phase-gate follow-up landed:
  - `StrategyDash.Phase` now hard-gates `2 = GRID FORMATION` on `DataCorePlugin.GameRawData.Telemetry.IsOnTrackCar==true` in addition to existing grid/formation session-state authority;
  - `3 = RACE` authority remains unchanged;
  - when not race-running and not on-track/in-car, phase now remains `1 = PLANNING` (grid/formation helper flags alone no longer promote phase 2).
- 2026-05-05 Planner-save pit-loss learning-mode overwrite fix landed:
  - `SavePlannerDataToProfile()` now updates `PitLaneLossSeconds` + `PitLaneLossSource/manual` + `PitLaneLossLearningMode/manual` only when the planner pit-loss value actually changes versus the stored track value;
  - ordinary planner saves that do not edit pit loss now preserve existing learned mode metadata (including `boxed_stop`) and avoid accidental normalization drift.

- 2026-05-05 Profiles manager pit-loss manual-edit consistency fix landed:
  - Profiles-tab pit-loss text edits now stamp `PitLaneLossLearningMode="manual"` alongside `PitLaneLossSource="manual"`;
  - prevents stale `boxed_stop` mode from persisting after manual pit-loss edits and incorrectly subtracting transition allowance in normalized pit-loss exports.

- 2026-05-04 Pit-loss mode/source consistency fix landed:
  - FuelCalcs track-save overwrite now also stamps `PitLaneLossSource="manual"` and `PitLaneLossLearningMode="manual"` when writing `PitLaneLossSeconds`;
  - prevents stale `boxed_stop` mode from persisting across manual/planner pit-loss overwrites and incorrectly subtracting transition allowance in normalized pit-loss exports.

- 2026-05-04 Pit-loss source-aware normalization follow-up landed:
  - added persisted `PitLaneLossLearningMode` metadata (`drive_through` / `boxed_stop` / `manual`) on saved track pit-loss records;
  - `Fuel.Live.PitLaneLoss_S` and `TrackLearning.PitLoss.ValueSec/Display` now expose drive-through-equivalent pit-lane loss (boxed-stop-learned values subtract fixed transition allowance and clamp at `0`);
  - shared boxed-stop seam now uses fixed transition allowance `+2.00s`; `Fuel.Live.TotalStopLoss` composes from normalized drive-through-equivalent loss + boxed-service model (+repair-aware) + transition allowance without double-counting.

- 2026-05-04 League Race Final Behaviour phase landed:
  - `ClassLeader.*` and `ClassBest.*` race-context class cohort matching now flows through the existing League Class resolver delegate seam (enabled+valid uses effective class; disabled/unresolved-player falls back to unchanged native class behavior).
  - `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, and `PitExit.Ahead/Behind.*` now use the same race-context class seam as Opponents cohort selection for enabled+valid League Class mode, with native fallback unchanged.
  - preserved invariants: no CarSA changes, no `H2HTrack` changes, no pit-exit countdown/loss/progress/gap formula changes.
  - PR #669 follow-up: in enabled+valid League effective-class mode, class-leader selection now chooses the lowest positive `CarIdxPosition` across the full effective-class cohort (race order) instead of first matching array index; native class-position fallback remains when overall position data is unavailable.
- 2026-05-04 Strategy tab race-configuration ownership cleanup landed:
  - Race Preset selector is now hidden during `Live Detect` ownership and preset modified UI is suppressed in that mode.
  - Live Detect owner transitions now preserve selected/applied preset state and preserve manual/preset setup values; only race-basis authority changes while Live Detect is selected.
  - Strategy calculation ownership remains effective-basis-only; no fuel model/live detect detection logic changes and no Refresh Calcs ownership mutation.

- 2026-05-04 Strategy race-ownership follow-up (P1 review) landed:
  - Live Detect background refresh now updates cached detected race basis/helper state without mutating manual `RaceLaps`/`RaceMinutes` when Live Detect is not selected.
  - Live Detect exit now also clears detected lap/minute cache values to prevent stale detected-length reuse across mode transitions.

- 2026-05-04 StrategyDash phase compile-fix follow-up (PR #660) landed:
  - `UpdateStrategyDashAdvice(...)` phase detection now takes explicit real-state booleans from the existing pre-race call path (`isRaceRunning`, `isGridOrFormation`) instead of referencing removed non-existent fields;
  - StrategyDash phase contract is preserved (`1=PLANNING`, `2=GRID FORMATION`, `3=RACE`) with grid+formation still combined;
  - no changes were made to fuel data/source/mode behavior, `PitFuelControlEngine`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, boxed refuel latches, or `Pit.FuelControl.*` semantics.

- 2026-05-04 League Race header alignment follow-up landed:
  - fixed class-table header offset by reserving a fixed checkbox column width (`18`) in both header grids and row grids for Detected classes and Fallback rules;
  - keeps existing bindings/tooltips/edit semantics unchanged (layout-only alignment correction).

- 2026-05-04 League Race settings table alignment polish landed:
  - League Class settings now use fixed-column grid headers for Detected classes, Player race class, and Fallback rules to align headers directly above their input columns;
  - Enabled/Colour Preview header labels were removed while keeping the checkbox and colour-preview controls;
  - added concise UI tooltips for CSV Class Name, Match Suffix, Short Name, Rank, and Colour Hex without changing existing bindings or resolver/runtime behavior.
- 2026-05-04 Offline data module toggle landed:
  - added plugin action `OfflineDataModule_Toggle` (debug-toggle style persisted flip) for dash control workflows;
  - added exported property `OfflineDataModule` (`0/1`) so dashboards can gate visibility directly from plugin state;
  - no plugin bindings-section UI row was added (action/property are surfaced via SimHub action/property surfaces).
- 2026-05-04 Strategy Live Detect effective-basis/runtime-refresh fix landed:
  - strategy planner now computes from an explicit effective race basis/length seam (manual or Live Detect) and never silently defaults Live Detect-null basis to Lap-Limited/manual laps;
  - timed Live Detect strategy paths (fuel total/stints/first-stint/after-zero) now consistently use detected minutes, while lap-limited Live Detect uses detected laps;
  - Live Detect definition updates now force strategy recalculation on basis/value/availability helper changes when Live Detect is selected;
  - session-id/type transitions now perform an immediate Live Detect refresh when Live Detect is selected, reducing stale detection on initial load/context transitions.

- 2026-05-04 Strategy Live Detect P1/P2 review follow-up landed:
  - current-session race definition acceptance now requires matching limit flags (`IsLimitedSessionLaps` for laps, `IsLimitedTime` for timed) in addition to positive values;
  - `SafeReadLong` now range-checks decimal values before casting, so out-of-range decimals safely return fallback instead of throwing.

- 2026-05-04 Strategy Live Detect P1 review follow-up landed:
  - fallback scan of `Sessions01..64` now still runs when `CurrentSessionInfo.IsRace==true` but current-session race length is missing/invalid, allowing fallback recovery of valid declared race definitions;
  - Live Detect lap-count reads now use tolerant parsing for current-session and SessionsXX (`_SessionLaps` then `SessionLaps`) so invalid/null/string values degrade safely to `0` in telemetry path.

- 2026-05-04 Strategy Live Detect race-definition source fix landed:
  - race-definition detection now prioritizes `CurrentSessionInfo` only when `CurrentSessionInfo.IsRace==true` and reads underscore session length authority first (`_SessionLaps`/`_SessionTime`), with non-underscore compatibility fallback after underscore attempts;
  - `Sessions01..64` fallback applies only when current session race data is unavailable/non-race/lengthless, and helper text now reports `race found, no valid length` when a race is visible but unusable;
  - bounded result-change logging now includes source/session/limit/raw-length fields with selected basis/reason for Live Detect diagnostics.

- 2026-05-03 League Race CSV detected-classes editing layer landed:
  - added persisted user-editable class-definition layer (`LeagueClassDefinitions`) keyed by CSV class name, with editable `Enabled`, `ShortName`, `Rank`, and `ColourHex`;
  - detected-classes table now binds to settings-owned editable rows; CSV class name remains read-only while enabled/rank/short/colour fields are editable with live colour preview;
  - resolver ownership remains split: CSV still maps `CustomerId -> raw CSV ClassName`; resolver then applies class-definition metadata for effective short name/rank/colour/valid;
  - rank semantics are now explicit (`1 = fastest/highest`, no reverse ordering), and missing/invalid CSV rank defaults to stable detected-order ranks starting at `1`;
  - disabled detected classes now resolve invalid in League Class path and safely fall back to native class behavior; manual player override path remains independent;
  - CSV reload preserves edits by class-name key for existing rows, adds defaults for new class names, and drops removed class names (settings list mirrors currently detected CSV classes).

- 2026-05-04 League Race detected-classes UI binding/notification fix landed:
  - `LaunchPluginSettings.LeagueClassDefinitions` now raises `PropertyChanged` when CSV reload replaces the list, so WPF `ItemsControl` refreshes rows immediately;
  - `LeagueClassDefinition` now implements `INotifyPropertyChanged` for `Enabled`, `ShortName`, `Rank`, and `ColourHex` so row edits update UI bindings/live preview consistently;
  - plugin now subscribes to League Class definition row changes to save settings immediately and refresh resolver-consumer preview paths without restart;
  - scope remains UI/settings/resolver-binding only (no Opponents/H2H/CarSA/PitExit/ClassLeader cohort ownership changes).


- 2026-05-03 Build triage follow-up (PR range #647-#652):
  - fixed Fuel per Lap helper TextBlock XAML compile break by removing duplicate Style assignment in `FuelCalculatorView.xaml` while preserving existing source-text trigger behavior (`FuelPerLapSourceInfo` / Profile / Live);
  - confirmed `InvertBooleanConverter` and `LapTimeValidationRule` class/namespace wiring are valid in-project; reported lookup errors were downstream/designer fallout from the XAML parse failure;
  - fixed League Class race-context delegate accessibility seam by making `OpponentsEngine.NativeCarRow` publicly accessible to match the public delegate signature consumed by `LalaLaunch` (`IsRaceContextClassMatch`), with no opponent ordering/filtering logic changes.

- 2026-05-03 Strategy planner-basis/condition-notify follow-up landed:
  - effective track-condition visibility properties now notify on SelectedTrackCondition changes;
  - planner/live session-match snapshot now uses Live Detect effective basis/length (or reports non-comparable until detected basis exists).

- 2026-05-03 Strategy Live Detect follow-up landed:
  - Live Detect now recomputes strategy when detected basis changes (lap/time) even when numeric values are unchanged;
  - race-session scan now checks all `Sessions01..64` race rows, prefers valid lap-limited definitions, and falls back to timed only when needed.

- 2026-05-03 Strategy ownership-binding follow-up landed:
  - Track Condition and Race Type radio groups now bind to ownership-only states (no mixed effective-state double-selection);
  - effective race-basis visibility remains separate while Live Detect is selected;
  - Live Detect session scan now uses safe read/tolerant parsing for SessionInfo loop inputs.

- 2026-05-03 Strategy tab UI bundle (#512, #628, #633) landed:
  - Fuel-per-lap helper text now renders below input to prevent narrow-width clipping; fuel source buttons/calcs unchanged;
  - Track Condition control now includes explicit Auto/Dry/Wet ownership with matching helper labels (`Automatic (...)` / `Manual override: ...`);
  - Race Type now includes persistent Live Detect ownership that reads declared race metadata from `SessionInfo.Sessions01..64` (`IsRace==true`) and locks race-length edits while active.
- 2026-05-03 League Race Phase 3 landed:
  - Opponents race-context cohort seam now supports resolver-backed League Class matching when enabled and player effective class is valid (player effective class vs opponent effective class);
  - disabled mode and enabled+unresolved-player mode both fall back to unchanged native class-color cohort behavior;
  - H2HRace target identity follows the same updated Opponents race-context seam; H2HTrack remains unchanged;
  - added plugin action `LeagueClass.ToggleEnabled` (registered as `LalaLaunch.LeagueClass.ToggleEnabled`) to toggle `Settings.LeagueClassEnabled` using existing enable-time mode guard and normal settings save flow;
  - no CarSA physical selection/filter/order changes, no H2HTrack changes, no PitExit class-row filtering changes, no ClassLeader/ClassBest changes.

- 2026-05-02 League Class startup guard regression fix landed:
  - `ApplyLeagueClassEnableModeGuard()` now auto-corrects `LeagueClassMode` `0 -> 3` whenever `LeagueClassEnabled==true`, covering both runtime enable-edge (`false -> true`) and legacy startup/load enabled+mode0 states;
  - preserves user-selected modes `1/2/3` and keeps `LeagueClassEnabled==false` behavior unchanged;
  - scope remains settings/UI guard only (no resolver/export/Opponents/PitExit/H2H/CarSA/ClassBest changes).

- 2026-05-01 League Class disabled-mode UI/state cleanup landed:
  - League Class settings now hide the entire lower configuration area while disabled, showing only the section header, master enable toggle, and disabled helper text;
  - CSV browse now writes directly to persisted `Settings.LeagueClassCsvPath` and immediately mirrors the chosen value in the textbox;
  - Reload action now no-ops while disabled by UI flow (control hidden with disabled section) and explicit click-guard;
  - re-enable preserves persisted CSV/fallback/manual settings, with existing enable-time mode guard (`Disabled` -> `CsvThenName`) unchanged.

- 2026-04-30 Pit Fuel Control Push/Save mode UI/control-surface pass landed:
  - added `Push/Save Profile Mode` toggle in Dash Control -> Global Dash Functions -> Fuel (OFF=`LIVE`, ON=`PROFILE`) bound to `Settings.PitFuelControlPushSaveMode`;
  - selector shares the existing setting with `Pit.FuelControl.PushSaveModeCycle` action path (no UI-local independent state);
  - notification hardening ensures `Pit.FuelControl.PushSaveModeCycle` programmatic changes immediately notify both int/bool settings bindings used by the open plugin toggle UI;
  - added Pit Commands settings binding row `Push/Save Mode Cycle` under Pit Fuel Control for `LalaLaunch.Pit.FuelControl.PushSaveModeCycle`;
  - no PitFuelControlEngine behavior/fuel-target math/guard/transport changes were introduced.

- Runtime fuel pit-space cap authority fix (2026-04-29):
  - live pit-space exports (`Fuel.Pit.TankSpaceAvailable`, `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`) now resolve cap from runtime live tank authority first (`EffectiveLiveMaxTank` seam);
  - stale Strategy/Profile `MaxFuelOverride` no longer silently clamps runtime pit-space when live cap authority exists;
  - safe fallback retained: planner/profile cap is used only when live cap authority is unavailable;
  - planner semantics unchanged: Profile/Live Snapshot planner max-fuel ownership, preset apply behavior, and max-fuel UI display behavior remain intact.

- 2026-05-01 League Class enable-toggle live visibility hotfix landed:
  - `LaunchPluginSettings.LeagueClassEnabled` now raises `OnPropertyChanged(nameof(LeagueClassEnabled))` via backing-field setter so enable/disable visibility bindings refresh live without restart;
  - League Class settings change hook now refreshes dependent plugin UI properties (`LeagueClassStatus`, `LeagueClassPlayerPreviewText`, `LeagueClassShowCsvSection`, `LeagueClassShowFallbackSection`) when enable/mode changes;
  - existing enable-edge mode guard remains active (`Disabled (0)` -> `CsvThenName (3)` only on `false -> true`).



# Repository status

Validated against commit: HEAD
Last updated: 2026-05-07
Branch: work

## Current repo/link status
- Local branch present: `work`.
- No Git remote is configured in this checkout (`git remote -v` returns empty).

## Documentation sync status
- 2026-05-04 StrategyDash IsOnTrackCar phase-gate follow-up landed:
  - `StrategyDash.Phase` now hard-gates `2 = GRID FORMATION` on `DataCorePlugin.GameRawData.Telemetry.IsOnTrackCar==true` in addition to existing grid/formation session-state authority;
  - `3 = RACE` authority remains unchanged;
  - when not race-running and not on-track/in-car, phase now remains `1 = PLANNING` (grid/formation helper flags alone no longer promote phase 2).

- 2026-05-04 League Race Final Behaviour phase landed:
  - `ClassLeader.*` and `ClassBest.*` race-context class cohort matching now flows through the existing League Class resolver delegate seam (enabled+valid uses effective class; disabled/unresolved-player falls back to unchanged native class behavior).
  - `PitExit.PredictedPositionInClass`, `PitExit.CarsAheadAfterPitCount`, and `PitExit.Ahead/Behind.*` now use the same race-context class seam as Opponents cohort selection for enabled+valid League Class mode, with native fallback unchanged.
  - preserved invariants: no CarSA changes, no `H2HTrack` changes, no pit-exit countdown/loss/progress/gap formula changes.
  - PR #669 follow-up: in enabled+valid League effective-class mode, class-leader selection now chooses the lowest positive `CarIdxPosition` across the full effective-class cohort (race order) instead of first matching array index; native class-position fallback remains when overall position data is unavailable.

- 2026-05-04 StrategyDash one-stop burn-basis current-tick refresh fix landed:
  - one-stop `StrategyDash.NextRefuelTargetLitres` PUSH/SAVE selection now uses current-tick locally resolved burns in `UpdatePreRaceOutputs(...)` (not shared prior-frame `PushFuelPerLap`/`FuelSaveFuelPerLap` fields at that stage);
  - fallback remains NORM when source is STBY/OFF/invalid or selected burn is unavailable;
  - preserved invariants: no behavior changes to `PitFuelControlEngine`, `Fuel.Delta.*`, `Fuel.RequiredBurnToEnd*`, `Fuel.Pit.*`, boxed refuel latches, or `Pit.FuelControl.*` exports/semantics.
- 2026-05-04 StrategyDash V2 seam + PreRace contingency-basis correction synced (legacy PreRace/Fuel/Pit/Pit.FuelControl exports retained).
- 2026-05-04 PreRace current-fuel setup-fallback follow-up landed:
  - `UpdatePreRaceOutputs(...)` now resolves effective current fuel as live fuel when valid/positive, else setup fallback (`Fuel.Setup.FuelLevel` when valid), else `0`;
  - setup fallback is gated to pre-race/grid/formation only (SessionState `<4`); during active race-running (SessionState `==4`) setup fallback is disabled and live fuel remains authoritative even when live fuel is `0`;
  - PreRace delta/status/feasibility paths now consume that effective PreRace-only current-fuel basis (one-stop/no-stop/multi-stop checks);
  - scope is PreRace-only: no overwrite of live telemetry fuel and no changes to live fuel learning, `Fuel.Pit.*` runtime math, planner calculations, or max-tank authority.

- 2026-05-04 Setup fuel fallback export landed (with litre-unit string validation follow-up):
  - added new read-only fuel setup exports `Fuel.Setup.FuelLevel`, `Fuel.Setup.FuelLevelValid`, `Fuel.Setup.FuelLevelSource`;
  - setup resolver checks setup paths in strict priority (`BrakesDriveUnit` -> `Chassis.Front` -> `Chassis.Rear` -> `Suspension.Rear`), accepts numeric raw values as litres, and accepts string values only when explicitly litre-labelled (`77.0 L`, `77 L`, `77,0 L`, `litre/litres/liter/liters`);
  - known non-litre/unsafe string units (for example `gal`/`gallon`) and bare numeric strings are rejected (no implicit conversion);
  - invalid/null/non-positive values now publish `Fuel.Setup.FuelLevel=0`, `Fuel.Setup.FuelLevelValid=false`, `Fuel.Setup.FuelLevelSource=none`;
  - preserved invariants: no overwrite of live telemetry fuel and no changes to live fuel model, pit math, planner math, PreRace strategy outputs, or max-tank authority.

- 2026-05-03 League Race Phase 3 PR #651 review follow-up fixes landed:
  - fixed `Opponents.NativeRaceModel.GetBlendedPaceForPosition(...)` compile blocker by persisting the in-scope race-context class-match delegate from `Build(...)` and reusing that stored delegate in pace lookup (no cohort behavior redesign);
  - fixed enabled+manual League Class edge where player row could become unmatched by forcing self-row class-match pass in the race-context delegate (`player identity == candidate identity`) before resolver-based opponent matching;
  - preserves resolver-based effective-class opponent matching semantics and native class-color fallback behavior outside enabled+valid League Class mode.

- 2026-05-01 League Race Phase 2 compile blocker hotfix landed:
  - fixed undefined-symbol build break in `LeagueClass.Player.*` export attachments (removed references to undeclared `_liveLeagueClassIdentityCustomerId/_liveLeagueClassIdentityName`);
  - exports now resolve live player identity through existing `TryGetLivePlayerIdentityPreview(...)` + resolver helper seam;
  - behavior remains passive/resolver-backed with no opponent/CarSA/H2H selection changes.
- 2026-05-01 League Race Phase 2 debug/metadata export pass landed:
  - added global `LeagueClass.*` status + player metadata exports (enabled/mode/status/counters + player name/short/rank/colour/valid/source/override);
  - added passive resolver-backed League Class metadata per Opp slots `Opp.Ahead1..5` / `Opp.Behind1..5`;
  - added optional passive resolver-backed League Class metadata per CarSA physical slots `Car.Ahead01..05` / `Car.Behind01..05`;
  - no Opponents selection/grouping changes, no CarSA ordering/filtering changes, no H2HRace/H2HTrack/ClassLeader/ClassBest behavior changes.

- 2026-05-01 League Race Phase 1 player override colour preview UI fix landed:
  - fixed Player race class preview swatch binding in `GlobalSettingsView.xaml` to use the same hex-to-brush conversion path as CSV/fallback previews via the override colour hex textbox source;
  - invalid/blank override hex remains safe and renders transparent;
  - no resolver/settings/export semantic changes were introduced.

- Runtime fuel pit-space cap authority fix (2026-04-29):
  - live pit-space exports (`Fuel.Pit.TankSpaceAvailable`, `Fuel.Pit.WillAdd`, `Fuel.Pit.FuelOnExit`) now resolve cap from runtime live tank authority first (`EffectiveLiveMaxTank` seam);
  - stale Strategy/Profile `MaxFuelOverride` no longer silently clamps runtime pit-space when live cap authority exists;
  - safe fallback retained: planner/profile cap is used only when live cap authority is unavailable;
  - planner semantics unchanged: Profile/Live Snapshot planner max-fuel ownership, preset apply behavior, and max-fuel UI display behavior remain intact.

- 2026-04-30 Race finish/end-phase authority redesign landed:
  - finish phase now exports `Race.EndPhase`, `Race.EndPhaseText`, `Race.EndPhaseConfidence`, and `Race.LastLapLikely` for dash-safe race-end gating;
  - `SessionState` is now primary authority for timed-race overall leader completion (`SessionState>=5`) and end-phase classification;
  - single-class effective/class leader finish now mirrors overall finish latch, while multiclass class-finish remains class-targeted;
  - session checkered flag is no longer used as proof of overall leader finish; player checkered remains driver-side timing/summary context only;
  - finish latches now resist single-tick non-race telemetry blips via sustained non-race reset guard.


- 2026-04-30 League Class enable-time mode guard landed:
  - added a narrow runtime guard that auto-corrects `LeagueClassMode` from `Disabled (0)` to `CsvThenName (3)` only when `LeagueClassEnabled` transitions from false to true;
  - preserves all existing user-selected non-disabled modes;
  - ensures League Class preview/resolution activates immediately when users enable League Class without touching mode.

- 2026-04-30 Build-fix pass landed (compile-only):
  - fixed League Class CSV browse dialog disposal compile error by removing `using` wrapper around `OpenFileDialog`;
  - fixed Push/Save mode-cycle feedback accessibility by routing through a new public `PitCommandEngine.PublishInfoMessage(...)` wrapper;
  - fixed League Class settings initializer legality by setting `LaunchPluginSettings.LeagueClassMode` default to literal `0`;
  - no intended behavior changes to pit-command transport, feedback hold/severity semantics, or League Class runtime logic.
- 2026-04-30 League Race Class Phase 1 cleanup pass landed:
  - player preview now uses live player identity when available (CustomerId/UserID + name), with explicit not-available wording while telemetry identity is missing;
  - CSV reload now handles read exceptions safely and reports non-fatal status text instead of throwing during init/reload;
  - League Race colour preview uses safe hex-to-brush conversion fallback (transparent on invalid/blank);
  - fallback-rule settings normalization now guarantees a stable 3-row editable collection.

- 2026-04-30 League Race Class Phase 1 infrastructure landed:
  - added settings/UI scaffold for League Class enable/mode, CSV path + reload, player override fields, and suffix fallback rules;
  - added resolver/cache infrastructure with CSV counters/status and preview resolution;
  - default remains disabled and no runtime class-cohort behavior injection was applied yet.


- 2026-04-30 Pit Fuel Control Push/Save profile-assisted mode landed:
  - added mode/action/exports for Push/Save burn basis (`LIVE`/`PROFILE`);
  - added configurable profile guard setting (`0..30%`, default 10%) in Global Dash Functions fuel settings;
  - only internal Pit Fuel Control PUSH/SAVE target composition is affected; NORM and PLAN behavior remain unchanged;
  - fallback is silent to existing live PUSH/SAVE behavior when profile/track/condition/guard inputs are invalid;
  - mode toggle now refresh-sends current target immediately in MAN/AUTO when source is PUSH/SAVE (no send for OFF/STBY/NORM/PLAN);
  - explicit toggle refresh send failures now follow SourceCycle-style fallback (`PIT CMD FAIL`, force `STBY`, AUTO disarm).
- 2026-04-29 Auto PreRace stable-source provenance follow-up landed:
  - in Auto (`selectedStrategy == 3`) when `LiveFuelPerLap_Stable > 0`, `LalaLaunch.PreRace.FuelSource` now follows the selected stable source only (`Live => live`, `Profile => profile`, other/non-standard stable source => `fallback`);
  - removed profile-baseline availability inference from this stable-consumption path so non-profile stable fallback cannot be mislabeled as `profile`;
  - preserved Auto fallback branch behavior for `LiveFuelPerLap_Stable <= 0` (direct profile-fuel selection still labels `profile` when selected).

- 2026-04-29 PreRace fuel-source label clarification + Auto profile fallback-source correction landed:
  - `LalaLaunch.PreRace.FuelSource` legacy labels `planner` and `simhub` were removed from runtime output;
  - accepted fuel-source outputs are now `live`, `profile`, `planner-profile`, `planner-manual`, `fallback`;
  - Auto mode remains runtime-only (`live`/`profile`/`fallback`) and does not emit planner-classified labels;
  - manual PreRace modes now classify planner-driven fuel source via existing `FuelCalculator` ownership state (`IsFuelPerLapManual`, `FuelPerLapSourceInfo`);
  - fixed Auto-mode source-label seam so profile baseline availability resolves to `profile` (not false `fallback`) when stable source text is non-live/non-profile.
- 2026-04-28 Lap-based contingency basis-scaling follow-up landed:
  - tactical `Fuel.Delta.LitresCurrent/Plan/WillAdd` and Push/Save required-to-finish paths now resolve contingency litres per matching burn basis when contingency is configured in laps (stable for Normal, push burn for Push, save burn for Save);
  - preserved public `Fuel.Contingency.Litres/Laps/Source` exports as stable-basis display/debug values;
  - fixed push/save reserve protection bias from reusing a single stable-basis contingency litre value across all tactical modes.
- 2026-04-28 Pit Fuel Control contingency double-count follow-up landed:
  - corrected live `NORM/PUSH/SAVE` target composition in `BuildPitFuelControlSnapshot()` so `Pit.FuelControl.TargetLitres` consumes `-Fuel.Delta.LitresCurrent*` directly with non-negative clamp;
  - removed contingency re-add in Pit Fuel Control live target path because tactical deltas already protect contingency on required-to-finish;
  - preserved planner `SAVED` target ownership (`PlannerNextAddLitres`) and all existing tactical delta semantics.
- 2026-04-28 Burn-to-end + contingency tactical fuel guidance update landed:
  - added new runtime exports: `Fuel.RequiredBurnToEnd`, `Fuel.RequiredBurnToEnd.Valid`, `Fuel.RequiredBurnToEnd.State`, `Fuel.RequiredBurnToEnd.StateText`, `Fuel.RequiredBurnToEnd.Source`, `Fuel.Contingency.Litres`, `Fuel.Contingency.Laps`, `Fuel.Contingency.Source`;
  - tactical `Fuel.Delta.LitresCurrent/Plan/WillAdd` and Push/Save variants now protect active contingency reserve on the required-to-finish side only;
  - contingency authority now resolves planner-first, then profile track fallback, then default `1.5 laps` fallback (zero contingency remains valid);
  - preserved invariants: `Fuel.Pit.WillAdd` clamp semantics, pit-window outputs, pit-stop count seams, stint burn target/band, predictor outputs, PreRace `FuelDelta/Stints`, and pit command/control engines.
- 2026-04-28 Pit Fuel/Tyre fault-pipeline consistency pass landed:
  - Fuel fault request-bit evaluation is now ownership-gated (`IsAutoModeActive || AutoArmed`) and external mirror/takeover handling explicitly clears pending owned mirror expectations;
  - closes stale owned-request leakage so post-surrender mirror states cannot keep `Pit.FuelControl.Fault=2` latched after ownership transitions;
  - Tyre AUTO correction-send ticks now force `Pit.TyreControl.Fault=0` in the same tick as correction issue (post-handler settle evaluation retained);
  - Tyre unmappable requested-compound truth (`service selected + requested compound present + !hasTruth`) now hard-suppresses fault to `0` across mode paths;
  - no command payload/timing/retry/transport/fuel-math/tyre-mode behavior changes were introduced.
- 2026-04-28 Pit Tyre AUTO-correction settle-window fault timing hotfix landed:
  - `PitTyreControlEngine.OnTelemetryTick()` now re-checks settle-window state after `HandleAuto(...)` and before `Pit.TyreControl.Fault` assignment;
  - AUTO correction-send ticks are now suppressed to `Pit.TyreControl.Fault = 0` as intended by settle-window gating;
  - no command behavior/AUTO logic/retry/payload changes were introduced.
- 2026-04-28 Pit Fuel/Tyre diagnostic fault timing follow-up landed:
  - Fuel and Tyre fault exports now compute from final post-tick state after same-tick mirror/remap/cancel handling (no pre-remap stale fault publish);
  - Fuel suppresses fault to `0` on the tick that applies external mirror remap (`AUTO REFUEL CANCELLED BY MFD` / OFF-MAN mirror remap path);
  - Tyre suppresses fault to `0` on truth-mirror remap / AUTO cancel remap ticks so legitimate mirror transitions do not flash one-tick non-zero faults;
  - no command payload, mode-remap behavior, retries, or correction-send behavior changes were introduced.
- 2026-04-28 Pit Tyre Control DRY/WET fault follow-up landed:
  - `PitTyreControlEngine.ComputeFault(...)` now returns `0` when DRY/WET diagnostic evaluation has requested-compound telemetry but cannot map truth into known dry/wet family (`hasTruth == false`);
  - known DRY/WET truth mismatch after settle still raises diagnostic fault bits as before;
  - no tyre command payload, AUTO behavior, or truth-mirror behavior changes were introduced.
- 2026-04-28 Pit Fuel/Tyre selector diagnostic fault exports landed:
  - added `Pit.FuelControl.Fault` and `Pit.TyreControl.Fault` (`0=None`, `1=Mode fault`, `2=Source/request fault`, `3=Mode + Source/request fault`) for dash-visibility diagnostics only;
  - fault evaluation is suppressed during existing post-command settle/suppression windows and unknown-truth windows to avoid normal command-latency flash;
  - no fuel/tyre command payload, transport, retry, or correction-send behavior changes were introduced.
- 2026-04-28 Issue #552 follow-up landed for max-fuel display ownership cleanup:
  - `RaisePresetStateChanged()` no longer raises `MaxFuelOverrideDisplayValue` notifications;
  - max-fuel display ownership is now explicit: Profile display follows authoritative `MaxFuelOverride`, Live Snapshot display follows live cap branch;
  - no strategy fuel math, preset application semantics, or Live Snapshot lock behavior changes.
- 2026-04-28 Issue #552 fix landed for Strategy max-fuel override control sync:
  - in Profile mode, Max Fuel Override slider/textbox now stay bound to the authoritative planner value after preset apply and during manual drag/edit;
  - helper percent text and strategy calculations continue to update from the same authoritative value;
  - Live Snapshot lock/authority behavior is unchanged (control remains live-cap-owned and non-editable).
- 2026-04-24 PR #626 follow-up visual contract correction landed:
  - pit feedback dash mapping now documents `Caution` as steady (no blink) and `Warning` as blink for 1 second at 750ms;
  - no `PitCommandEngine` runtime behavior changes were made in this correction.
- 2026-04-24 PR #626 follow-up landed for pit feedback severity-priority gating:
  - `PitCommandEngine` feedback publisher now suppresses lower-severity incoming feedback while a higher-severity message is active;
  - equal/higher-severity incoming feedback still replaces immediately and restarts the active hold window (including repeated identical message retrigger cases);
  - no payload/transport/timing/fuel/tyre AUTO behavior changes were introduced, and no counters/sequence fields were added.
  - pit feedback docs/contracts now include the explicit dash visual mapping (`None/Info/Advisory/Caution/Warning`) and logic CSV contracts include a `Severity` column with consistent uppercase `PIT CMD FAIL`.
- 2026-04-24 follow-up hardened pit feedback reset seams:
  - removed `PitCommandEngine.ResetFeedbackState()` calls from `Telemetry.IsOnTrackCar` edge handlers in `LalaLaunch`;
  - pit command feedback reset now remains on explicit lifecycle/reset seams only (for example manual/runtime reset flows), avoiding transient on-track telemetry-gap clears while command feedback is active.
- 2026-04-24 Pit command feedback severity standardization landed:
  - `PitCommandEngine` now owns unified pit feedback severity classification and exports `Pit.Command.Severity` (`0..4`) + `Pit.Command.SeverityText` (`None/Info/Advisory/Caution/Warning`);
  - `Pit.Command.Active` remains restartable-per-publish and now uses a standardized 3000ms hold window, including retrigger for repeated identical `Pit.Command.DisplayText`;
  - reset seams now clear pit feedback display/active hold and reset severity to `None` without changing `LastAction/LastRaw` semantics;
  - Fuel Control, Tyre Control, built-in pit commands, custom messages, and raw command feedback continue to flow through `PitCommandEngine` publisher ownership.
- 2026-04-24 Tyre Control review-follow-up landed for send-failure feedback hold:
  - `PitTyreControlEngine` now tracks a short send-failure hold timestamp when raw tyre command send returns false;
  - outside AUTO, truth-mirror mode remap still occurs as before, but passive mirror feedback publish (`TYRE OFF/DRY/WET`) is suppressed only during the failure-hold window so `PIT CMD FAIL` remains visible;
  - command payloads and mode-cycle behavior are unchanged (`#cleartires`, `#t tc 0`, `#t tc 2`; `OFF->DRY->WET->AUTO->OFF`).
- 2026-04-24 Tyre Control feedback wording + command-active retrigger follow-up landed:
  - plugin-driven Tyre Control feedback now uses CHANGE wording (`TYRE CHANGE OFF/DRY/WET/AUTO`);
  - AUTO correction feedback now uses `TYRE AUTO CHANGE DRY/WET`;
  - AUTO manual takeover feedback now uses `TYRE AUTO CANCELLED`;
  - outside AUTO truth-mirror remaps now publish passive state wording (`TYRE OFF/DRY/WET`) only when mirrored mode actually changes (no passive spam after plugin-driven sends);
  - `Pit.Command.Active` behavior confirmed unchanged and already restartable per publish: every feedback publish restarts the active display window even when `Pit.Command.DisplayText` is identical.
- 2026-04-24 feedback polish follow-up landed for Pit Fuel Control:
  - owned-mirror expectation expiry now skips changed telemetry dimensions for that tick, so plugin-owned `OFF -> MAN` `#fuel$` ON echoes are consumed as owned (no false `REFUEL SET ON BY MFD` external message);
  - no-change expectation expiry behavior is retained to prevent stale pending-owned masking of later genuine manual MFD edits;
  - MAN over-space feedback wording now includes source/requested litres (`REFUEL <SRC> <requested>L >MAX`), while AUTO wording remains `AUTO FUEL <requested>L >MAX`;
  - command payloads, transport, AUTO external-cancel behavior, and fuel maths are unchanged.
- 2026-04-24 suppression gate fix landed for Pit Fuel Control frozen-action follow-up:
  - `BuildPitFuelControlSnapshot(...)` no longer blanket-suppresses Fuel Control in Offline Testing; suppression now gates only truly invalid snapshot contexts (`no-plugin-manager`, `no-session`);
  - snapshot now carries suppression reason for diagnostics (`SuppressFuelControlReason`), and Fuel Control entry logs now expose `suppressReason=<...>`;
  - action blocked suppression logs now include reason (`suppressed:<reason>`), and telemetry suppression logging is transition/throttled with explicit suppression-clear transition log (no per-tick suppression spam).
- 2026-04-24 frozen-action diagnostics instrumentation landed for Pit Fuel Control action path tracing:
  - `LalaLaunch` Fuel Control actions now log entry receipts before engine calls (`PitFuelControl* action received`) to prove SimHub action binding reachability;
  - `PitFuelControlEngine` action entry points now log compact state snapshots, and action-path early returns now log explicit blocked reasons (`snapshot-null`, `suppressed`, `off-hard-guard`, `auto-plan-blocked`, `plan-invalid`, `source-stby`, `target-invalid`, `send-failed`, `auto-not-armed`, `lap-cross-no-material-delta`, `iracing-autofuel-ownership`, `external-mirror-change`, `owned-mirror-consumed`);
  - telemetry tick diagnostics remain transition/reason based only (no per-tick spam), and command semantics/payloads were not changed.
- 2026-04-24 Tyre Control truth-mirror telemetry family mapping fix landed:
  - `PitTyreControlEngine.IsRequestedCompoundInDesiredFamily(...)` now maps requested compound telemetry as `0 => DRY` and `1 => WET` for truth-mirror / AUTO truth classification.
  - outgoing tyre chat commands are unchanged (`DRY => #t tc 0`, `WET => #t tc 2`); only telemetry-family interpretation changed.
- 2026-04-24 Tyre Control AUTO-entry follow-up landed (pending initial evaluation no longer clears on unknown truth):
  - `PitTyreControlEngine.HandleAuto(...)` now preserves `_autoPendingInitialEvaluation` when AUTO is entered/evaluated with unknown tyre truth (`hasTruth == false`);
  - AUTO initial-evaluation path no longer updates `_autoLastDesiredWet` while truth is unknown, preventing suppression of the later first known-truth evaluation;
  - when truth becomes known, AUTO performs its normal one-shot first evaluation (single correction send on mismatch, pending clear with no send when already matching);
  - one-shot/no-retry command model remains unchanged; no new retry/guard/counter ownership was introduced.
- 2026-04-24 Tyre Control command-model simplification landed (single-send truth-following model):
  - `PitTyreControlEngine` now uses one-shot combined commands (`OFF => #cleartires`, `DRY => #t tc 0`, `WET => #t tc 2`) with transport-owned `$` normalization unchanged; standalone `#t`/`#tc` split logic is removed from tyre control.
  - removed tyre retry/attempt/timeout-fail state machines and plugin-owned suppression/intent-grace tracking; replaced with a single 1.0s settle hold after plugin-issued tyre commands.
  - outside AUTO, mode now mirrors known MFD truth only after settle (`OFF` / dry-family / wet-family) and never sends corrective commands for manual pit-menu tyre edits.
  - AUTO now enters feedback-only (`TYRE AUTO`), performs one correction send only when known MFD truth mismatches declared-wet target, and cancels/remaps on manual takeover with `TYRE AUTO CANCELLED` (no fight-back send).
  - tyre-control `PIT CMD FAIL` is now transport-failure only (raw send returned false); timeout-driven failure/revert paths were removed.
- 2026-04-24 review follow-up landed for Fuel Control impossible-state ModeCycle handling:
  - `PitFuelControlEngine.ModeCycle()` now guards impossible `AUTO + PLAN` before AUTO->OFF send logic;
  - impossible branch now recovers to `Source=STBY` + `AutoArmed=false`, remains AUTO/disarmed, and sends no command;
  - closes residual mismatch where `AUTO + PLAN + ModeCycle` could still send `#-fuel$` despite CSV no-send recovery row.
- 2026-04-23 Pit Fuel Control contract-alignment follow-up landed (direct-command model preserved; feedback/guard rows aligned):
  - `PitFuelControlEngine` now keeps source-send feedback on `MAN -> AUTO` (`AUTO REFUEL SET <SRC> X L`) and no longer overwrites with generic mode text;
  - AUTO source sends now use AUTO wording (`AUTO REFUEL SET ...`) and AUTO max/over-space wording (`AUTO FUEL <requested>L >MAX`), while MAN max feedback remains `FUEL MAX`;
  - `MAN STBY/PLAN -> AUTO STBY` now publishes `AUTO REFUEL STBY`;
  - `AUTO -> OFF` now publishes `REFUEL OFF` while still sending explicit `#-fuel$`;
  - invalid MAN `SetPlan` (planner/live mismatch) now publishes `Pit Cmd Fail` instead of silent no-op;
  - impossible `AUTO + PLAN` state is now guarded to no-send recovery (`AUTO STBY`, disarmed) and cannot fall through to PUSH send.
  - `Pit.ToggleFuel` action remains unchanged/available as an independent manual binding and is not used by Fuel Control ownership logic.
- 2026-04-23 Pit Fuel Control testing/polish pass landed (command payload fix + observability + table alignment):
  - `PitFuelControlEngine.ModeCycle()` OFF->MAN sends `#fuel$` (no `#+fuel$` additive form) and uses `FUEL MAN STBY` feedback.
  - `PitCommandEngine.ExecuteRawPitCommand(...)` empty-after-normalization blocked path now logs both raw and normalized payload text for diagnosis.
  - `Docs/Subsystems/FuelModesLogicCSV.csv` updated to reflect OFF->MAN payload fix and explicit MAN/AUTO over-tank-space max-feedback rows (`FUEL MAX`, `AUTO FUEL <requested>L >MAX`) with no outgoing-payload clamp redesign.
- 2026-04-23 Tyre Control follow-up landed (compound confirmation timeout no longer reverts successful DRY/WET MFD changes):
  - `PitTyreControlEngine.EnsureCompound(...)` pending confirmation now succeeds immediately when requested compound truth exists and matches desired DRY/WET family (`HasRequestedCompound` + family match), without waiting for tyre-service ON confirmation;
  - successful family convergence now clears pending compound confirmation state before timeout evaluation, preventing false timeout failure from undoing already-successful MFD compound changes;
  - bounded timeout failure handling remains active only for truly unconfirmed windows (`PIT CMD FAIL` + manual truth remap when no requested-family convergence occurred).
- 2026-04-23 Pit command transport regression fix landed (chat-open `T` leak guard):
  - `PitCommandEngine` now force-closes chat (`Esc`) before opener (`T`) in both direct-postmessage and legacy-sendinput chat-injection paths;
  - prevents stale-open chat from absorbing the opener key into outgoing typed payloads (`t#...` / `tt#...`) for raw/custom pit commands such as `#tc 0`;
  - scope remained transport-only (no tyre/fuel control logic redesign).
- 2026-04-23 PR follow-up landed for Fuel Control owned-mirror expectation expiry:
  - pending owned requested-fuel / fuel-fill expectations are now cleared whenever currently observed telemetry already equals the queued expected values, even without a same-tick change event;
  - closes stale-pending ownership attribution after suppression-window or baseline-init convergence, preventing later manual MFD same-value edits from being masked as plugin-owned.
- 2026-04-23 PR follow-up bundle landed for Fuel Control review findings (reviewed commit `91cc94a494`):
  - `SetPlan` blocked in AUTO now cleanly no-ops without disarming AUTO (`Source`/`AutoArmed` unchanged on blocked AUTO press);
  - Fuel Control now sends explicit OFF/MAN MFD state commands (OFF->MAN sends `#+fuel$`; AUTO->OFF sends `#-fuel$` and preserves AUTO state on send failure);
  - MAN-owned plugin sends are tracked so delayed telemetry echoes are consumed as owned mirror updates (not reclassified as external `FUEL CHANGED BY MFD`);
  - PLAN send path restored hard validity gating so planner/live mismatch or invalid PLAN cannot fall through to unintended sends.
- 2026-04-23 authoritative Fuel Control behavior-table alignment landed:
  - `PitFuelControlEngine` now enforces OFF isolation (`OFF STBY` no-send source/set actions), MAN-only PLAN action semantics (`Pit.FuelControl.SetPlan`), and AUTO PLAN inhibition in switching logic;
  - `ModeCycle` now mirrors table contract: `OFF -> MAN STBY` no send, `MAN -> AUTO` send only for `PUSH/NORM/SAVE`, `AUTO -> OFF` always attempts raw `#-fuel$` and falls back to `AUTO STBY` on send failure;
  - external MFD/fuel-request changes are now explicit mirror-only messaging (`AUTO REFUEL CANCELLED BY MFD`, `REFUEL SET OFF BY MFD`, `REFUEL SET ON BY MFD`, `FUEL CHANGED BY MFD`) with OFF/MAN STBY mirror state;
  - no retries/poll loops were added and no toggle semantics were reintroduced into Fuel Control.
- 2026-04-23 Tyre Control PR review follow-up landed (restore service-ON intent tracking for `#tc` sends):
  - `PitTyreControlEngine.EnsureCompound(...)` now marks pending service intent (`desiredSelected=true`) alongside pending compound intent whenever DRY/WET/AUTO issues `#tc ...$`;
  - delayed OFF->ON tyre-service truth convergence caused by successful `#tc` remains protected as plugin-owned intent even after compound family confirmation is already complete;
  - keeps the simplified command model unchanged (`OFF => #cleartires$`; `DRY/WET/AUTO => #tc ...$` only), with no `#t$` reintroduction and no retry-loop changes.
- 2026-04-23 Tyre Control PR review follow-up landed (compound confirmation success-path restore):
  - while `_compoundConfirmationPending` is active, `PitTyreControlEngine.EnsureCompound(...)` now first checks whether `PitSvTireCompound` has converged into the requested DRY/WET family before considering timeout failure;
  - successful family convergence now clears pending compound confirmation state and pending compound-intent tracking immediately;
  - timeout unconfirmed fallback (`PIT CMD FAIL` + manual-truth remap) now occurs only when the confirmation window actually expires without requested-family convergence;
  - keeps the always-send single-command `#tc ...$` model and no-retry-loop behavior unchanged.
- 2026-04-23 Tyre Control PR review follow-up landed (always-send compound intent for DRY/WET/AUTO):
  - removed `PitTyreControlEngine.EnsureCompound(...)` "already correct compound family => no send" short-circuit;
  - DRY/WET/AUTO now always issue a single `#tc ...$` command on mode-intent transitions/AUTO enforcement events, regardless of current requested-compound family truth;
  - preserves single-send + short confirmation-window semantics with no retry loops and no `#t$` service-on path reintroduction;
  - closes the service-OFF recovery gap where family-match no-op suppressed command emission and could let manual/AUTO fall back to OFF.
- 2026-04-23 Tyre Control simplification follow-up landed (single-send confirmation, no `#t$`, no retries):
  - tyre control command model now uses `OFF => #cleartires$` and `DRY/WET/AUTO => #tc ...$` only (internal `#t$` sequencing removed);
  - tyre service/compound resend loops and retry/cooldown attempt budgets were removed for tyre control;
  - each manual action/AUTO target change now performs one send attempt and waits a short bounded confirmation window (~900 ms);
  - unconfirmed timeout (or immediate send failure) now publishes `PIT CMD FAIL`, then remaps mode to current authoritative MFD truth with no retry loop;
  - preserves existing mode cycle and AUTO external/manual takeover-cancel behaviour.
- 2026-04-23 LapRef/PB reliability follow-up landed (bounded seam fix):
  - PB writes now execute on the accepted validated-lap seam in `UpdateLiveFuelCalcs` using the same authoritative lap-time handoff as LapRef validated capture (instead of waiting for native best-lap event timing);
  - added a bounded second `UpdateLapReferenceContext(...)` pass in the 500ms cadence immediately after accepted-lap processing so SessionBest/ProfileBest handoff visibility does not lag an extra update cycle;
  - periodic native best-lap seam remains for readback refresh only; condition-specific wet/dry PB routing and optional sector persistence semantics remain unchanged.
- 2026-04-23 PR follow-up restored `OFF -> MAN` progression for ModeCycle-only Fuel Control bindings:
  - `PitFuelControlEngine.ModeCycle()` OFF branch now issues an explicit fuel-amount command attempt (`#fuel ...$`) using the selected source target instead of selection-only state mutation;
  - keeps Fuel Control ownership explicit-command based (no `Pit.ToggleFuel` / `#!fuel` reintroduction) while allowing non-AUTO MAN truth to advance via MFD telemetry `dpFuelFill`;
  - transport/send failure remains visible via existing `Pit Cmd Fail` feedback semantics.
- 2026-04-23 Pit Fuel Control mode ownership refactor landed (explicit-command model, no internal toggle dependency):
  - `PitFuelControlEngine` no longer depends on `_fuelToggleSender`, `TryToggleFuelFillEnabled(...)`, or `NotifyPluginFuelToggleAction()` for Fuel Control mode ownership;
  - Fuel Control mode cycle now uses explicit command semantics only: `OFF -> MAN` is selection-only intent with no send, `MAN -> AUTO` keeps existing immediate amount-send ownership behavior, and `AUTO -> OFF` sends explicit raw OFF command `#-fuel$` (single attempt, no retry/poll loop);
  - successful AUTO->OFF explicit send exits AUTO to `Source=STBY` + `AutoArmed=false`; local transport failure remains visible as `Pit Cmd Fail`;
  - OFF hard guard, AUTO cancel edge-trigger behavior, on-track reset seam, and lap-cross AUTO cadence remain unchanged;
  - direct plugin action `Pit.ToggleFuel` remains available as a separate built-in pit action outside Fuel Control ownership.
- 2026-04-23 PR review follow-up restored non-blocking post-toggle verification in `TryToggleFuelFillEnabled(...)`:
  - `_fuelToggleSender()` transport-attempt success is no longer treated as confirmed toggle by itself;
  - after successful send attempt, the engine now performs one immediate snapshot read and requires `dpFuelFill` to match expected ON/OFF before returning success;
  - no wait/re-poll loops were reintroduced; snapshot unavailable/mismatch returns failure so `ModeCycle` does not advance on unconfirmed toggle.
- Pit Fuel Control regression follow-up landed (`OFF -> MAN` ModeCycle freeze fix):
  - removed redundant blocking telemetry re-poll loop from `PitFuelControlEngine.TryToggleFuelFillEnabled(...)` after `Pit.ToggleFuel`;
  - `Pit.ToggleFuel` remains the authoritative state-confirmed toggle seam (before/after `dpFuelFill` check in `PitCommandEngine`);
  - preserves existing non-blocking behavior while keeping toggle-result reporting aligned to immediate telemetry truth during `OFF -> MAN`.
- Tyre Control regression follow-up landed (manual mode-change confirmation window restore):
  - restored `BeginOrClearManualConfirmation(mode)` arming in `PitTyreControlEngine.SetMode(...)`;
  - prevents immediate manual-truth reconciliation from remapping fresh manual mode selections to stale MFD truth before first enforcement send attempts;
  - restores expected tyre control-engine send attempts after `ModeCycle`/`SetDry`/`SetWet` manual actions while keeping existing bounded reconcile fallback behavior.
- Tyre Control PR review follow-up landed (complete AUTO intent match + unknown service-enforcement hold):
  - AUTO plugin-owned delayed convergence now requires full relevant pending-intent agreement (`service` and `compound` when both are pending/relevant), preventing single-dimension matches from masking external/manual takeover;
  - tyre-service enforcement retries are now held while service truth is unknown/unavailable (`HasTireServiceSelection=false`), so telemetry gaps do not burn bounded retry budget;
  - scope remained narrow: no transport, command-model, or mode-cycle redesign.
- Tyre Control follow-up bundle landed (build fix + authoritative service truth + AUTO cancel hardening):
  - fixed `PitTyreControlEngine.OnTelemetryTick()` local-variable collision build break (`desiredWet`);
  - tyre-service truth availability is now authoritative only when all four tyre-change flags are available (partial/missing flag telemetry is unknown/non-authoritative);
  - outside AUTO manual truth mapping now only consumes authoritative four-flag service truth (no partial/LF fallback authority);
  - AUTO external-ownership cancel no longer forces `OFF` on ambiguous/unavailable truth;
  - AUTO now includes delayed plugin-intent convergence protection so staged plugin-originated truth updates arriving after immediate suppression are not misclassified as external takeover;
  - genuine external/manual MFD takeover still cancels AUTO (`TYRE AUTO CANCELLED`) and remaps to concrete manual truth.
- 2026-04-23 PR follow-up fixed AUTO-exit OFF toggle guard:
  - `PitFuelControlEngine.ModeCycle()` AUTO-exit now checks live `dpFuelFill` truth before sending `Pit.ToggleFuel` OFF,
  - if fill is already OFF, AUTO exits to OFF state without sending a toggle (`Source=STBY`, AUTO cleared/disarmed),
  - OFF toggle send still runs only when fill is currently ON; bounded verification + `Pit Cmd Fail` mismatch feedback behavior remains unchanged.
- 2026-04-22 Pit Fuel Control V2 follow-up polish landed:
  - `ModeCycle` now explicitly drives effective mode loop `OFF -> MAN -> AUTO -> OFF` and actively toggles MFD fuel-fill truth on OFF/MAN transitions (`Pit.ToggleFuel` ON/OFF with bounded `dpFuelFill` validation),
  - toggle validation mismatches now publish `Pit Cmd Fail` and fall back to actual MFD truth without correction looping,
  - OFF is now a hard safety guard: source actions (`SourceCycle`, `SetPush`, `SetNorm`, `SetSave`) cannot send fuel commands while effective mode is OFF,
  - AUTO entry now sends immediately for `PUSH/NORM/SAVE` and arms only on successful send; PLAN entry is one-shot immediate send and then forced back to `Source=STBY` disarmed,
  - AUTO source cycle is now PLAN-isolated (`PUSH -> NORM -> SAVE -> PUSH` only); PLAN remains available in MAN cycle only,
  - AUTO cancel edge-trigger and lap-cross AUTO update cadence remain unchanged.
  - same-day follow-up: MAN->AUTO immediate-send failures now explicitly publish `Pit Cmd Fail` for both `SAVED` and `PUSH/NORM/SAVE` entry branches while preserving fallback-to-`STBY` + disarmed behavior.
- Tyre Control PR follow-up landed (explicit raw-command model + AUTO external ownership cancel/remap):
  - tyre control engine no longer uses internal toggle semantics for service enforcement (`Pit.ToggleTyresAll` remains a direct user action only);
  - explicit tyre commands are now authoritative in-engine (`OFF => #cleartires$`; `DRY/WET/AUTO => #t$` then dry/wet `#tc ...$`);
  - AUTO ownership now includes bounded plugin-owned suppression windows for post-send MFD changes;
  - MFD tyre changes outside suppression are treated as external takeover, publish `TYRE AUTO CANCELLED`, and remap mode out of AUTO to current manual truth (`OFF`/`DRY`/`WET`);
  - four-tyre service truth + tri-state ambiguous hold behavior remain intact (no collapse of unknown truth to OFF).
- Tyre Control PR follow-up landed (tri-state manual truth mapping for tyre-service telemetry gaps):
  - outside AUTO manual truth mapping now treats tyre-service state as tri-state (`ON` / `OFF` / `UNKNOWN`) instead of collapsing unavailable service telemetry to OFF;
  - unknown tyre-service truth now returns ambiguous/no-truth for manual reconciliation (no forced OFF remap during telemetry gaps; fail-safe hold behavior preserved);
  - confirmed OFF still maps to `OFF`, and service ON still maps to `DRY/WET` only when requested-compound family truth is valid.
- Tyre Control PR follow-up landed (outside-AUTO ownership ordering + OFF reset latch fix):
  - outside AUTO (`OFF`/`DRY`/`WET`), manual truth reconciliation now runs before manual enforcement so stale manual mode intent is not re-applied ahead of external MFD truth;
  - `ResetToOff()` safety resets now stay latched at `OFF` on the next telemetry tick (no immediate `OFF -> DRY/WET` truth-remap regression);
  - AUTO ownership behavior and AUTO unconfirmed info-feedback behavior remain unchanged.
- Tyre Control follow-up landed (manual 2-way MFD truth sync outside AUTO + AUTO info-only unconfirmed policy):
  - outside AUTO (`OFF`/`DRY`/`WET`), plugin mode now runs a bounded 2-way truth-sync contract against all-four tyre service truth + requested compound truth (`PitSvTireCompound`), including post-request confirmation fallback to actual MFD truth and bounded external-change remap;
  - manual truth mapping remains constrained to existing families (`service OFF => OFF`, `service ON + dry-family => DRY`, `service ON + wet-family => WET`) with fail-safe hold when truth is ambiguous;
  - AUTO remains authoritative ownership mode even when bounded enforcement attempts are unconfirmed;
  - unconfirmed AUTO enforcement now publishes visible info-level feedback/logging (`TYRE AUTO UNCONFIRMED`) without collapsing mode out of AUTO;
  - existing bounded retry/cooldown and transport ownership seams remain unchanged (no second transport path).
- 2026-04-22 docs sweep for v1.1 release prep completed:
  - refreshed root `CHANGELOG.md` unreleased `v1.1` notes to be concise and user-facing,
  - reviewed and refreshed `Docs/User_Guide.md` and `Docs/Quick_Start.md` pit/custom command guidance links,
  - added canonical subsystem contract doc `Docs/Subsystems/Pit_Commands_And_Fuel_Control.md` to capture combined pit command + fuel/tyre control ownership,
  - updated `Docs/Project_Index.md` subsystem map to include the new pit command subsystem doc.
- Pit Fuel Control v2 redesign landed (AUTO plugin-owned, OFF/MAN MFD-derived truth):
  - effective `Pit.FuelControl.Mode` now derives from iRacing MFD fuel-enable truth (`dpFuelFill`) whenever plugin AUTO is not active (`OFF` when unchecked, `MAN` when checked);
  - AUTO cancel no longer uses mismatch/baseline comparison and is now edge-triggered on external changes to either requested fuel (`PitSvFuel`) or fuel-enable (`dpFuelFill`);
  - external (non-plugin-owned) changes still cancel AUTO once and force safety recovery state (`Source=STBY`, `AutoArmed=false`) with short feedback `AUTO CANCELLED`;
  - plugin-owned changes are ignored by cancel detection only when they come from plugin raw `#fuel` sends or explicit plugin `Pit.ToggleFuel` actions;
  - `Telemetry.IsOnTrackCar` reset seam remains authoritative and now resets AUTO/STBY ownership state without forcing plugin-owned OFF/MAN mode values.
- Tyre Control v1 follow-up landed (service-state contract fix + retry hardening + Pit Commands UI tidy-up):
  - `PitTyreControlEngine` service-state truth now reuses the same all-four tyre-selection seam used by `Pit.ToggleTyresAll` confirmation (`PitCommandEngine.ReadTyresAllState`), replacing prior any-tyre truth for control enforcement;
  - tyre-control enforcement contract is now explicit: service ON only when all four tyre-change flags are selected; partial selections are treated as OFF;
  - compound send attempts now always consume retry/cooldown budget even on local raw-send failure, keeping retries bounded and non-spammy on failure paths;
  - Settings → Pit Commands removed preview auto-focus toggle and now shows only `Tyre Mode Cycle` for tyre control binding UI;
  - direct `Pit.TyreControl.SetOff/SetDry/SetWet/SetAuto` actions remain registered for SimHub Controls & Events / Dash Studio use.
- Plugin-owned tyre control v1 landed with focused helper ownership (`PitTyreControlEngine`) and existing pit transport seam reuse:
  - added persistent tyre mode contract (`OFF` / `DRY` / `WET` / `AUTO`) with mode-cycle order `OFF -> DRY -> WET -> AUTO -> OFF`;
  - mode reset now reuses existing `Telemetry.IsOnTrackCar` edge reset seam + session reset seam and forces `OFF` on each edge;
  - runtime target behavior now enforces: `OFF` -> tyre service OFF; `DRY` -> service ON + dry compound target; `WET` -> service ON + wet compound target; `AUTO` -> service ON + declared-wet authority targeting;
  - immediate requested-state verification seam uses `Telemetry.PitSvTireCompound` family checks (dry `{0,1}` / wet `{2,3}`), bounded by cooldown + attempt limits (no per-tick spam/infinite resend);
  - compound-change attempts now emit explicit observability with requested/fitted/weather seams and available GT3-first compound context (`DriverTires01/02.TireCompoundType`);
  - added plugin actions `Pit.TyreControl.ModeCycle/SetOff/SetDry/SetWet/SetAuto` and dash exports `Pit.TyreControl.Mode` + `Pit.TyreControl.ModeText` (settings UI now intentionally exposes only `Tyre Mode Cycle` row for tyre control).
- PR follow-up fixed PB condition-only readback fallback in 500ms best-lap refresh path:
  - when a best-lap event cannot be validated against the current accepted-lap handoff, condition-only PB readback now uses live `_isWetMode` instead of stale `_lastValidLapWasWet`;
  - validated events continue to use accepted-lap wet latch, preserving write/read consistency on accepted laps.
- Wet lap-time / wet PB persistence write-path audit follow-up:
  - accepted-lap wet/dry routing is now latched through downstream PB write/readback gating, preventing condition drift between lap validation and PB persistence;
  - wet tyre routing now treats positive native `PlayerTireCompound` values as wet (`>0`) to avoid false-dry routing on non-zero wet compound variants;
  - profile avg-lap persistence (`AvgLapTimeDry/Wet`) now follows pace-accepted laps directly (no longer blocked by fuel-accepted gate), restoring wet lap-time feed for Strategy/Profile consumers;
  - PB write gating now treats cleared non-positive baseline values as unavailable, allowing first valid wet PB relearn lap to persist after clear.
- Pit Fuel Control polish — feedback-only max-fill wording:
  - plugin-owned pit fuel sends keep original transport payload behavior (no outgoing litres clamp; max-override continues using additive overshoot payload),
  - max-fill style requests now use short user-facing feedback (`FUEL MAX`) when requested/sent litres exceed current tank space (or max-override path is active), using raw tank-space comparison so sub-1L overshoot still resolves as max-fill feedback,
  - normal non-max requests keep litres-based feedback text,
  - command ownership, transport mode behavior, and AUTO cancel behavior remain unchanged.
- PreRace follow-up clarified one-stop feasibility ownership in code:
  - introduced explicit helper `IsOneStopFeasibleForPreRace(...)` for one-stop gate evaluation,
  - helper evaluates one-stop against pit-stop refill capacity plus second-stint fuel demand (`total needed - start fuel`),
  - scenario-first decision ordering and all status outcomes remain unchanged.
- PR follow-up corrected PreRace one-stop feasibility physical gate:
  - one-stop feasibility no longer uses race-start free space (`tank - currentFuel`) as the stop-fill cap,
  - one-stop feasibility now uses pit-stop refill capacity (effective tank capacity at stop) before normal underfuel/overfuel checks,
  - scenario-first status ordering and all other status behavior remain unchanged.
- PreRace v2 scenario-first + grid live-delta/source-contract follow-up:
  - PreRace status logic now classifies required strategy first (`no-stop` / `one-stop` / `multi-stop`) and then evaluates selected strategy with mutually exclusive outcomes (eliminates prior no-stop/multi-stop fallthroughs),
  - one-stop feasibility now includes a tank-capacity gate (`fuelStillNeeded > maxFuelAddPossible` => red `ONE STOP NOT POSSIBLE`),
  - Auto now always follows required-strategy behavior and does not inherit manual-only `STRATEGY MISMATCH`,
  - PreRace one-stop fuel delta now consumes raw telemetry requested-fuel seam on grid so delta updates while dialing pit fuel request,
  - shared planner/live race-length matching tolerances relaxed to ±1 minute (timed) / ±1 lap (lap-limited),
  - Auto source labels remain runtime-owned (`live`/`profile`/`fallback`) and no longer expose planner ownership labels.
- Added plugin-owned `ClassBest.*` export family for the current player-class session-best lap holder:
  - new exports: `ClassBest.Valid`, `ClassBest.CarIdx`, `ClassBest.Name`, `ClassBest.AbbrevName`, `ClassBest.CarNumber`, `ClassBest.BestLapTimeSec`, `ClassBest.BestLapTime`, `ClassBest.GapToPlayerSec`.
  - holder resolution reuses the existing simplified trusted class-best seam (`TryResolveClassSessionBestLap`) already used by `H2H*.ClassSessionBestLapSec` / magenta session-best-in-class coloring.
  - identity resolution reuses existing session-info/native helper seams (`TryGetCarIdentityFromSessionInfo`, `TryGetCarDriverInfo`).
  - gap semantics reuse the existing class-leader live-gap seam (`TryGetCheckpointGapSec` when sane, otherwise progress/pace fallback via shared `ResolveClassGapToPlayerSec`).
  - fail-safe unresolved contract is explicit (`Valid=false`, `CarIdx=-1`, empty strings, `BestLapTimeSec=0`, `BestLapTime="-"`, `GapToPlayerSec=0`) and now clears alongside existing class-leader resets on non-eligible sessions.
- Direct pit/custom transport chat-state sequencing follow-up:
  - direct window-message path now logs staged attempt/abort telemetry (`chat-open`, `text-send`, `submit`) per command attempt,
  - direct path now tracks uncertain chat-open carryover and can suppress repeated chat-open keying (`chat-open-suppressed=state-maybe-open`) on next attempt,
  - `Auto` fallback is now intentionally suppressed after partial direct chat-state mutation (`fallback_suppressed=true reason=postmessage-partial-state-unsafe`) to prevent second-path chat corruption,
  - no autofocus/focus-steal work and no duplicate-send retry path were introduced; transport mode/action surfaces remain unchanged.
- Pit/custom transport follow-up truth-sync (semantics/observability only):
  - transport order remains unchanged (`Auto`: direct `postmessage` first, bounded legacy `sendinput` fallback only on direct-path failure),
  - no duplicate-send retry path was added after queued direct-message success,
  - pit-command success logs now separate transport attempt vs effect confirmation (`delivery=...`, `effect-confirmed=...`),
  - custom-message/raw-command/stateless built-ins now log transport-attempt success as unverified delivery; stateful built-ins keep before/after telemetry as authoritative confirmation.
- PR follow-up hardened pit/custom transport iRacing process authority to simulator-only matching:
  - `IsIracingProcessName(...)` now accepts only `iRacingSim64DX11` (case-insensitive),
  - both `IsIracingForeground()` and `TryResolveIracingMainWindow(...)` continue to share that helper,
  - transport/fallback seam behavior is otherwise unchanged (direct window-message first in `Auto`, bounded foreground `SendInput` fallback).
- Pit/custom command transport upgrade (bounded seam in `PitCommandEngine`):
  - new default transport mode is `Auto`: direct iRacing window-message send first (`postmessage`) with explicit fallback to legacy foreground `sendinput`;
  - added settings-level transport selector (`Auto`, `Legacy foreground SendInput only`, `Direct message only`) in `Settings -> Pit Commands`;
  - built-in pit actions, custom-message actions, raw pit-command seam, feedback exports, and stateful toggle confirmation ownership remain unchanged;
  - transport logs now explicitly report `transport=...`, fallback (`fallback_from=postmessage`), and bounded failure reasons (`no-iracing-process`, `no-iracing-window`, `not-foreground`, etc.).
- PreRace system refresh + shared planner/live validity seam:
  - extracted shared planner/live session match helper and moved Pit Fuel Control PLAN validity to it;
  - added `PlannerLiveSessionMatchHelper.cs` to explicit `LaunchPlugin.csproj` compile items for non-SDK project build inclusion;
  - PreRace Auto now uses live race-definition authority first (`_SessionTime` timed / `_SessionLaps` lap-limited) plus runtime stable fuel/lap source seams;
  - PreRace Auto status thresholds now map stints as: `<= 1.0` => `NO STOP OKAY`; `> 1.0` and `<= 2.0` => `SINGLE STOP OKAY`; `> 2.0` => existing multi-stop handling;
  - non-Auto PreRace now shows orange `STRATEGY MISMATCH` only when planner/live inputs are comparable and actually mismatch (`HasComparableInputs && !IsMatch`), so transient missing values do not raise mismatch;
  - replaced coarse PreRace status band with richer text + color output (`LalaLaunch.PreRace.StatusColour`: `green`/`orange`/`red`);
  - overfuel warning now requires excess > `2x` configured contingency.
  - PR follow-up fixed planner snapshot API accessibility mismatch by making `FuelCalcs.GetPlannerSessionMatchSnapshot()` internal to match internal snapshot type visibility.
- PR follow-up fix for Strategy live-cap freshness-window bypass:
  - tightened `TryGetRuntimeLiveCapForStrategy(...)` to remove unbounded cached-cap return paths (`LiveCarMaxFuel` / `EffectiveLiveMaxTank`);
  - Strategy live-cap authority now resolves through one bounded fallback seam only (`raw -> bounded fallback -> unavailable`), so stale cached caps cannot outlive fallback freshness gating.
- PR follow-up fix for runtime fuel health/recovery merge blockers:
  - moved pit-road telemetry read before active-driving runtime health edge logic so `isOnPitRoad` is defined before use;
  - tightened `ManualRecoveryReset(...)` planner-safe short-circuit so early return is allowed only during active live session when planner-safe recovery succeeds;
  - preserved planner-safe behavior for live runtime recovery while keeping non-live manual reset on the existing broad reset path.
- Runtime health/recovery stabilization sweep (fuel/live snapshot seam):
  - added bounded runtime health checks for live max tank seam (session token/type, combo, and car-active edges) with debounced recovery;
  - unified Strategy live-cap authority to plugin runtime live-cap seam (`raw -> bounded last-valid fallback`);
  - added planner-safe targeted manual recovery path for fuel/live snapshot refresh;
  - removed automatic planner manual-override reset from fuel-model session reset path to preserve planner intent during runtime re-arm;
  - synced Fuel Model / Pace & Projection / Fuel Planner docs and internal inventory/log/changelog notes to reflect the new bounded health/recovery behavior.
- Analysis-first class-resolution simplification landed with one trusted-property authority model and shared seams:
  - class-state authority for runtime consumers now uses `GameData.HasMultipleClassOpponents` directly;
  - single-class path now bypasses class matching entirely and uses overall leader / whole-field best directly;
  - multiclass path now uses one shared player-class seam for both class leader and class-best resolution;
  - `Race.ClassLeaderHasFinished*` now consumes the same class-leader seam used by `ClassLeader.*` (no separate finish-only resolver);
  - removed metadata-cache authority helpers and duplicate class authority trees from active class-leader/class-best/finish paths.
- Wet-condition PB/session-best audit follow-up (bounded to profile/PB/planner/LapRef wet-dry behavior):
  - planner/profile PB reads now use condition-only lookup, so wet mode no longer borrows dry PB when wet PB is absent;
  - validated wet PB persistence remains condition-scoped on existing telemetry gate path (`_isWetMode` -> `TryUpdatePBByCondition(...)` -> wet PB fields only);
  - LapRef session-best authority sync remains trusted-seam based but now only applies when near-equal to the captured current-context baseline, preventing dry global-best seam values from repopulating wet SessionBest after wet reset.
- Final documentation sweep aligned user-facing + dash-facing + internal docs with the implemented combined pit command stack:
  - plugin-owned pit command actions and plugin-owned custom-message actions are now the documented default workflow;
  - built-in pit command configuration location is documented as `Settings -> Pit Commands`;
  - custom-message configuration location is documented as `Settings -> Custom Messages`;
  - pit fuel control usage notes are aligned to the implemented plugin-owned source/mode/action/export behavior;
  - focus caveat remains explicit and truthful (`iRacing` foreground required for reliable pit/custom sends; auto-focus not implemented).
- Profile-track PB clear polish follow-up (bounded to profile-track editor clear path + UI text styling):
  - condition PB clear now immediately clears visible PB text in-place by mirroring relearn refresh semantics (clear PB display cache + raise property changed for condition PB text);
  - dry/wet clear button labels are now compact and identical: `Clear PB Data`;
  - condition `No sector data` status text now uses an orange warning-style colour for better visibility;
  - PB cleared-state semantics remain unchanged (`0`/non-positive still unavailable, no valid zero-time PB reference path).
- PR #584 follow-up fixed custom-message restart persistence at settings load:
  - removed eager default prepopulation of `LaunchPluginSettings.CustomMessages` before JSON deserialize;
  - `NormalizePitCommandSettings(...)` remains the default-slot authority and now supplies default rows only when collection data is missing/null/undersized;
  - saved custom-message rows now load intact across restart, while new/missing settings still normalize to the expected 10 slots.
- Pit Fuel Control follow-up (bounded to pit fuel-control + pit-command seam):
  - `Pit.FuelSetZero` and `Pit.FuelSetMax` ZERO phase now transport `#fuel 0.01$` (MAX phase unchanged).
  - AUTO cancel now uses live requested-fuel movement ownership detection outside plugin send suppression (no stale baseline mismatch dependence); cancellation is one-shot to `OFF + STBY` with `AUTO CANCELLED`.
  - Plugin AUTO now disengages when iRacing AutoFuel is enabled (no AUTO co-ownership).
  - External lifecycle reset now uses only `Telemetry.IsOnTrackCar` edge detection (`false -> true` and `true -> false`) and forces `OFF + STBY` on each edge.
  - Offline Testing now suppresses Pit Fuel Control to inert `OFF + STBY`.
- LapRef/H2H/profile-track follow-up (bounded scope):
  - profile-track editor now has condition-specific PB cleanup actions:
    - `Clear Dry PB + Sectors` (dry PB lap + dry S1..S6 only),
    - `Clear Wet PB + Sectors` (wet PB lap + wet S1..S6 only);
  - each condition block now shows `No sector data` only when that condition has a PB lap but incomplete/missing PB sector payload;
  - H2H unresolved class-best info log is now reason-transition latched (no cadence spam), with startup suppression for single-class metadata-not-ready state;
  - LapRef profile-best rematerialization is now condition-only for `LapRef.ProfileBest.*` (wet profile-best no longer falls back to dry PB).
- PR #585 follow-up fixed a high-priority startup authority regression in native class-state resolution:
  - unknown `WeekendInfo.NumCarClasses` no longer defaults to single-class when `HasMultipleClassOpponents` is unavailable/false;
  - authority now remains unresolved/fail-safe unless native single-class is explicit (`NumCarClasses == 1`) or a positive multiclass signal exists (`NumCarClasses > 1`, or unknown class-count with `HasMultipleClassOpponents == true`);
  - this preserves multiclass safety for class-best and class-leader consumers during metadata-lag windows and avoids transient cross-class leaders/best laps.
- Analysis-first class-resolution cleanup aligned native class authority and same-class matching across H2H, ClassLeader, session-best-in-class labeling, and finish-path class leader resolution:
  - introduced a single native authority seam (`NumCarClasses` primary, `HasMultipleClassOpponents` fallback only when class-count is unknown) and removed stored `_isMultiClassSession` state from finish-path decisions;
  - simplified effective same-class matching to one shared rule: explicit single-class sessions always match (blank/mismatch tolerated), multiclass sessions require usable matching class identity;
  - aligned finish-path class-leader selection to the same helper used by class-best/class-leader consumers, eliminating stricter ad hoc class-string checks in one path;
  - retained source-ordered class metadata population (`Drivers##` preferred, `CompetingDrivers[*]` bounded missing-entry backfill) and multiclass fail-safe behavior.
- PR #584 follow-up debounced `Settings -> Custom Messages` text-edit persistence to avoid full settings writes on each keystroke:
  - custom slot `Name` / `MessageText` edits now use a 500 ms settle window before save;
  - pending debounced saves are flushed during normal plugin tick and plugin shutdown to keep latest settled text persisted across SimHub restarts;
  - custom-message action names/binding surfaces and pit/custom command runtime behavior were intentionally unchanged.
- Fixed Settings -> Custom Messages persistence regression: custom slot label/message edits now save on change via settings-layer collection/item hooks, so values persist across SimHub restarts without altering action binding names or pit/custom command execution seams.
- PR #582 follow-up addressed final multiclass authority ordering review feedback:
  - `_isMultiClassSession` now applies native authority in strict order: explicit native single-class (`NumCarClasses == 1`) wins outright, explicit native multiclass stays next (`NumCarClasses > 1`, or unknown + `HasMultipleClassOpponents`), and cache diversity is only used when class-count authority is unresolved/unknown;
  - this prevents cache divergence (`CarClassShortName` vs `CarClassName`) from overriding an explicit native single-class session signal.
- PR #582 merged-follow-up addressed remaining review findings with tight class-metadata/cache scope:
  - class metadata cache still prefers `DriverInfo.Drivers##`, but `CompetingDrivers[*]` now always runs as a per-car missing-entry backfill pass (never overwriting already resolved class rows);
  - `_isMultiClassSession` still honors explicit native multiclass signals as primary, and now accepts already-built cache diversity (`>1` distinct non-blank class names) only when native class-count authority is unresolved/unknown;
  - unknown class-count startup safety remains intact (no multiclass inference from unresolved/blank state alone).
- PR #582 follow-up addressed two additional review findings without widening subsystem scope:
  - class metadata fallback now keys off actual metadata recovery (not only `Drivers##.CarIdx` row presence), so `CompetingDrivers[*]` is used when `Drivers##` exists but class names are still blank/late;
  - `_isMultiClassSession` now requires an explicit multiclass signal (`NumCarClasses > 1`, or unknown class-count with positive `HasMultipleClassOpponents`) instead of deriving from unresolved single-class checks, preventing finish-path multiclass side effects during startup.
- PR #582 follow-up fixed a single-class inference safety gap in class-best resolution:
  - `IsEffectivelySingleClassSession(...)` now treats unknown `WeekendInfo.NumCarClasses` state as unresolved/non-single-class instead of inferring single-class from an unset `HasMultipleClassOpponents` hint.
  - blank-class same-class fallback now requires explicit native single-class authority (`NumCarClasses == 1`), preventing transient whole-field class-best picks in multiclass metadata-startup windows.
- Restored native class-best resolution seam for H2H/ClassLeader in all live opponent-eligible sessions (Practice/Open Qualify/Lone Qualify/Qualifying/Race):
  - class metadata refresh now runs before class-best consumers in tick order (cache update -> class-best calculations -> H2H/ClassLeader publication paths),
  - metadata cache population now prefers `DriverInfo.Drivers##` with `CompetingDrivers[*]` fallback.
- Added conservative effective same-class fallback behavior for single-class sessions with blank class identity:
  - single-class authority uses native `WeekendInfo.NumCarClasses` (with `HasMultipleClassOpponents` as supporting hint),
  - blank class identity is accepted only for defensibly single-class matching,
  - multiclass sessions still fail safe when class identity is unusable (no cross-class collapse).
- Added bounded H2H info observability for class-best resolution miss reasons (`missing_or_late_class_metadata`, `blank_class_identity_multiclass`, `no_valid_best_laps`) while keeping existing one-time fallback-removal warning behavior.
- Added plugin-owned live-session `ClassLeader.*` export family for player-class session-best leader context:
  - new exports: validity/carIdx, identity (`Name`/`AbbrevName`/`CarNumber`), class-best lap (`BestLapTimeSec`/`BestLapTime`), and `GapToPlayerSec`.
  - class-best seam now resolves both best-lap seconds and winning car index from existing native `CarIdxBestLapTime` cache authority.
  - identity resolution reuses existing native/session-info helpers (`Drivers##` + `CompetingDrivers[*]` fallback seams).
  - gap semantics mirror existing live-gap behavior (prefer CarSA checkpoint gap when sane, otherwise native progress/pace fallback).
  - eligibility is live opponent sessions (Practice/Open Qualify/Lone Qualify/Qualifying/Race), with fail-safe invalid outputs when unresolved.
- PR review follow-up gated LapRef SessionBest authority sync by active context:
  - LapRef now disarms authoritative `playerBestLapTimeSec` sync on context reset (session/type/car/track/wet-dry), preventing stale prior-session best-lap carry-over from pre-seeding new-session SessionBest lap-time state.
  - first valid in-context `CaptureValidatedLap(...)` session-best capture re-arms authoritative sync, so trusted authority resumes driving `LapRef.SessionBest.LapTimeSec` only when context-valid.
  - this preserves LapRef ownership of session-best sector payload (`S1..S6*`) while restoring first-lap SessionBest sector capture behavior in new sessions.
- LapRef analysis-first follow-up fixed remaining parity gaps against trusted H2H/core seams:
  - `LapRef.SessionBest.LapTimeSec` no longer depends on LapRef-local validated-lap latch timing; it now synchronizes each tick from the same trusted player best-lap authority path used by H2H/core.
  - LapRef keeps session-best sector payload ownership (`S1..S6*`) from validated-lap capture, decoupled from session-best lap-time authority.
  - removed remaining LapRef-local lap-ref advance rollover latch dependency in current-lap compare re-arm, keeping rollover behavior aligned to live active-segment wrap seam.
  - `LapRef.Player.LapTimeSec` and player-row CarSA-cache-driven sector display behavior remain on the previously corrected trusted seams.
- PR #577 review follow-up cleared AUTO baseline state on disable:
  - `ModeCycle` still cycles `OFF -> MAN -> AUTO -> OFF`.
  - `AUTO -> OFF` now sets `Mode=OFF`, `AutoArmed=false`, `LastSentFuelLitres=-1`, and `Source=STBY`, with feedback `FUEL MODE OFF` and no send.
  - Existing MAN->AUTO forced-STBY guardrail behavior remains unchanged (`PLAN/STBY -> AUTO` keeps `AutoArmed=false` and feedback `FUEL AUTO STBY`).
- PR #576 follow-up fixed bounded Pit Fuel Control behavior regressions:
  - `Pit.FuelSetMax` tank-full short-circuit is now phase-aware: MAX phase can short-circuit, ZERO phase always transports.
  - forced-STBY mode transitions now preserve mode context in one feedback string:
    - `AUTO -> MAN` => `FUEL MAN STBY`
    - `MAN -> AUTO` from `SAVED` => `FUEL AUTO STBY`
  - `MAN -> AUTO` from `SAVED` now leaves `AutoArmed=false` while forcing `Source=STBY` (AUTO mode selected, but not armed until explicit live-source reselection/send).
  - `MAN -> AUTO` from `STBY` now also leaves `AutoArmed=false` and keeps feedback explicit as `FUEL AUTO STBY` (prevents immediate self-cancel before reselection/send).
- Pit Fuel Control control-model follow-up corrected action semantics:
  - `Pit.FuelSetMax` is now a real MAX/ZERO behavioral toggle on transport (`MAX -> ZERO -> MAX -> ZERO`), while `Pit.Command.FuelSetMaxToggleState` still flips on every press.
  - `ModeCycle` no longer hard-blocks `MAN -> AUTO` when `Source=SAVED`; it allows AUTO and immediately forces `Source=STBY` so the driver must reselect a live source.
  - Latest follow-up keeps that MAN->AUTO forced-STBY guardrail but restores disable flow by cycling `AUTO -> OFF` (no AUTO->MAN forced-STBY branch in current behavior).
  - `ModeCycle` selection feedback remains plugin-owned and explicit (`FUEL AUTO STBY` on forced-STBY MAN->AUTO, `FUEL MODE OFF` on AUTO->OFF).
- PR follow-up hardened LapRef authoritative-lap freshness at rollover:
  - `ResolveLapRefAuthoritativeLapTimeSec(...)` now validates `CarIdxLastLapTime` freshness against the validated-gate lap candidate before overriding.
  - when both are valid but diverge beyond a tight tolerance, capture/PB handoff now stays on the validated-gate candidate for that tick (prevents one-tick stale previous-lap overrides).
- LapRef timing-source and PB-trigger alignment fix landed:
  - LapRef validated-lap capture now resolves player lap time from authoritative `CarIdxLastLapTime` seam (same trusted player lap-time seam used by H2H/core), with guarded fallback only when unavailable.
  - `_lastValidLapMs` (PB trigger handoff) now latches from that same authoritative captured lap value, removing first-expected-beat misses caused by stale capture values.
  - `LapRef.Player.LapTimeSec` and `LapRef.SessionBest.LapTimeSec` now inherit the corrected capture seam through LapRef snapshot/session-best ownership.
- LapRef rollover presentation parity tightened to proven H2H feel:
  - player-row sector box refresh now consumes live CarSA fixed-sector cache presence directly (H2H-like display continuity),
  - compare/cumulative truth remains current-lap-only and re-arms on rollover as before.
- PB persistence invariants preserved:
  - PB lap-time update stays on existing `TryUpdatePBByCondition(...)` seam,
  - sector persistence remains conditional on real sector data only,
  - legacy lap-time-only PB rows remain valid.
- PR #575 follow-up fixed Pit Fuel Control source-change failure feedback masking:
  - `SourceCycle` and direct source-select actions now publish `FUEL SRC ...` selection feedback only when no send is attempted (`Mode=OFF`).
  - In `Mode=MAN/AUTO`, failed send attempts now keep existing pit-command failure feedback (`Pit Cmd Fail`) visible instead of being overwritten by selection text.
- PR follow-up corrected Pit Fuel Control live-target contingency ordering:
  - `NORM/PUSH/SAVE` now apply contingency before clamp (`max(0, shortfall + contingency)`)
  - avoids contingency-only overfuel commands when current fuel is already above the base live requirement
  - `NORM` now aligns with the same direct requirement-minus-current seam family used by `PUSH/SAVE` (`-Fuel_Delta_LitresCurrent*`)
- Corrected baseline fuel projection authority across race phases without adding new public property families:
  - SessionState `2/3` (grid/formation) now drives timed-race lookahead from `CurrentSessionInfo._SessionTime` with elapsed `Telemetry.SessionTime`
  - SessionState `4` keeps normal live running projection behavior
  - existing `Fuel.Delta.*` / `Fuel.Pit.*` outputs inherit the fix from the shared projection seam
- Remapped Pit Fuel Control source authority to non-clamped live seams:
  - `SAVED` stays planner-owned (`PlannerNextAddLitres`)
  - `NORM` stays non-clamped runtime need seam
  - `PUSH/SAVE` now use direct requirement-minus-current seams (no `WillAdd` coupling)
- Added live-source contingency alignment for Pit Fuel Control:
  - `PUSH/NORM/SAVE` now include profile contingency (`ContingencyValue` + `IsContingencyInLaps`)
  - explicit contingency zero remains zero (no hidden reserve)
- Hardened LapRef rollover seam for transient zero-segment boundary samples:
- LapRef player-side refactor now reuses H2H/core trusted seams directly:
  - `LapRef.Player.LapTimeSec` now publishes from the same trusted player last-lap seam used by H2H/core.
  - player sector row display now consumes live CarSA fixed-sector cache directly each tick (H2H-style) instead of a LapRef-local display/rollover snapshot lifecycle.
  - removed redundant LapRef-local player display state while retaining only minimal current-lap comparable state for truthful compare/cumulative outputs.
  - SessionBest/ProfileBest static reference ownership and PB persistence invariants remain unchanged.
  - current-lap compare/cumulative eligibility now re-arms on normal wrap (`current > 0 && previous > 0 && current < previous`) and on boundary transition into segment `0` from late-lap state (`previous > 1 && current == 0`)
  - closes the `6 -> 0 -> 1` mapping path so stale prior-lap compare/cumulative validity does not leak into new-lap start
  - kept player-row sector-box persistence behavior unchanged
- Finalized LapRef live cumulative delta rollover semantics after the earlier sector-box persistence patch:
  - kept player sector-box visual persistence across lap rollover
  - separated current-lap compare eligibility from display persistence
  - `LapRef.Compare.*` and top-level cumulative deltas now use current-lap comparable state only
  - at lap start/rollover, cumulative valid flags now re-arm false and cumulative values publish `0` until new-lap comparable sectors exist
- Removed dead rollover state from `LapReferenceEngine` (`_isLivePlayerLapRolloverArmed`) while retaining required rollover tracking (`_lastLivePlayerActiveSegment`) and current-lap comparable snapshot state.
- Kept profile-best fallback semantics unchanged, including legacy lap-time PB rows with missing sectors.
- Fixed LapRef live player sector presentation across lap rollover:
  - removed per-tick hard clear behavior from the live player comparison snapshot path
  - completed sector boxes now persist through start/finish and are replaced progressively as new-lap sectors complete
  - live-sector full clear remains tied to true LapRef reset conditions only (session/car/track/type/wet-dry/explicit reset)
- Added a narrow local lap-rollover rearm seam in `LapReferenceEngine` to keep internal current-lap capture clean without causing visible zero/empty flash at new-lap start.
- Kept profile-best fallback semantics unchanged (lap-time PB may still exist without sector payload).
- Corrected LapRef live-comparison semantics: per-sector compare and top-level cumulative deltas now use player **current-lap completed sectors** from live CarSA cache context, not `_playerSnapshot` last-validated-lap sectors.
- Kept references static: session-best and profile-best remain validated/reference snapshots with unchanged capture/persistence ownership.
- Pruned redundant LapRef per-reference active-segment exports:
  - kept `LapRef.ActiveSegment` and `LapRef.Player.ActiveSegment`
  - removed `LapRef.SessionBest.ActiveSegment` and `LapRef.ProfileBest.ActiveSegment`
- Active-segment publication is now intentionally lean: `LapRef.ActiveSegment` (family-level) and `LapRef.Player.ActiveSegment` (player row) provide the live highlight source.
- Added additive top-level cumulative LapRef delta exports with explicit validity guards:
  - `LapRef.DeltaToSessionBestSec`, `LapRef.DeltaToSessionBestValid`
  - `LapRef.DeltaToProfileBestSec`, `LapRef.DeltaToProfileBestValid`
- Kept existing LapRef per-sector compare outputs (`LapRef.Compare.*.S1..S6State/DeltaSec`) unchanged.
- Preserved boundaries: CarSA fixed-sector ownership unchanged, H2H/Opponents behavior unchanged, and profile PB persistence rules unchanged.
- Updated LapRef subsystem and parameter inventory docs to reflect the active-segment mirroring and cumulative-delta contract.
- Reworked pit command transport to direct chat-command injection while keeping plugin-owned pit action endpoints for Strategy Dash / PitPopUp (`LalaLaunch.Pit.*`).
- Expanded plugin-owned pit action set (`ClearTires`, ±1/±10 fuel steps, `FuelSetMax`, `ToggleAutoFuel`, `Windshield`) with compatibility aliases retained for `Pit.FuelAdd` and `Pit.FuelRemove`.
- Added short-lived pit command feedback exports (`Pit.Command.DisplayText`, `Pit.Command.Active`) plus low-cost diagnostics (`Pit.Command.LastAction`, `Pit.Command.LastRaw`).
- Added explicit failure handling:
  - chat injection unavailability/failure warnings,
  - before/after mismatch warnings for stateful toggles,
  - user-facing `Pit Cmd Fail` fallback text.
- Tightened pit-command logging semantics so transport-stage warnings are explicitly best-effort; state-confirmation mismatch remains the authoritative failure signal for stateful actions.
- Fuel-add feedback now uses explicit `Fuel MAX` wording for max/clamp cases using existing pit tank-space authority.
- Kept pit read-side authority unchanged: pit service status/selection remains telemetry-based (`dpFuelFill`, tyre selectors, `PlayerCarPitSvStatus` and related seams).
- Updated subsystem/user/internal docs and development changelog to match final direct-chat transport and feedback/failure contract.
- Added focused plugin-owned Pit Fuel Control state machine (`PitFuelControlEngine`) integrated into the existing Pit Command transport/feedback seam.
- Added plugin-owned Pit Fuel Control actions:
  - `Pit.FuelControl.SourceCycle`
  - `Pit.FuelControl.ModeCycle`
- Added plugin-owned Pit Fuel Control exports:
  - `Pit.FuelControl.Source` / `SourceText`
  - `Pit.FuelControl.Mode` / `ModeText`
  - `Pit.FuelControl.TargetLitres`
  - `Pit.FuelControl.OverrideActive`
- Implemented locked behavior contract: PLAN validity gating, MAN/AUTO immediate sends, AUTO lap-cross updates on the existing lap-cross detector seam, MAX override hysteresis, and AUTO cancel-to-`MAN+STBY` on manual driver fuel override.
- Kept dashboards as consumers only (no dashboard JSON/UI edits).
- PR 572 follow-up tightened PLAN validity further: car + track/layout key + race basis + race length now all must match between planner and live session before PLAN is considered valid.
- PR 572 follow-up moved pit fuel-control sends to a dedicated raw pit-command path that intentionally reuses built-in pit-command normalization before chat injection (trailing `$` consistency fix).
- PR 572 follow-up made MAX override command explicit with a named clamp-safe overshoot constant rather than implicit magic literals.
- Pit Fuel Control follow-up expanded action/feedback coverage:
  - `SourceCycle` and `ModeCycle` now update `Pit.Command.DisplayText` + `Pit.Command.LastAction` even on selection-only presses (no fuel send).
  - Added direct source-select actions: `Pit.FuelControl.SetPush`, `Pit.FuelControl.SetNorm`, `Pit.FuelControl.SetSave`.
  - Added plugin-owned zero-fuel action `Pit.FuelSetZero` on the normal pit-command transport/feedback seam.
  - `Pit.FuelSetMax` now flips a plugin-owned toggle concept state exported as `Pit.Command.FuelSetMaxToggleState` for dash label logic.

## Reviewed documentation set
### Changed in final docs sweep: plugin-owned pit commands + custom messages + pit fuel control
- `README.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in pit/custom command transport upgrade (direct window-message + bounded fallback)
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `GlobalSettingsView.xaml`
- `README.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in ClassLeader.* live-session export task
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef rollover seam transient-zero follow-up
- `LapReferenceEngine.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef timing-source + PB-trigger + rollover parity task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR follow-up: LapRef CarIdx freshness guard
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef active-segment + cumulative delta task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef cumulative-delta rollover truth follow-up
- `LapReferenceEngine.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef live rollover persistence task
- `LapReferenceEngine.cs`
- `Docs/Subsystems/LapRef.md`
### Changed in pit command polish + Settings expansion task
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `GlobalSettingsView.xaml`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Plugin_UI_Tooltips.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/RepoStatus.md`

### Changed in PR 570 cleanup follow-up task
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control integration task
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `LaunchPlugin.csproj`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in PR 572 follow-up correctness fixes
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control action/feedback follow-up
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control control-model follow-up (real max toggle + STBY mode-guardrails)
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in PR #576 follow-up (FuelSetMax ZERO-phase bypass + forced-STBY feedback refinement)
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in fuel projection phase seam + Pit Fuel Control authority alignment task
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Subsystems/Fuel_Model.md`
- `Docs/Subsystems/Pace_And_Projection.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR follow-up contingency-before-clamp correction
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef live-current comparison correction task
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR #584 follow-up (custom-message load normalization persistence fix)
- `LalaLaunch.cs`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Previously changed in plugin-owned pit command actions task
- `PitCommandEngine.cs`
- `LalaLaunch.cs`
- `LaunchPlugin.csproj`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Reviewed and left unchanged
- `Docs/Project_Index.md`
- `Docs/Internal/CODEX_CONTRACT.txt`
- `Docs/Internal/Architecture_Guardrails.md`
- `Docs/Subsystems/Pit_Timing_And_PitLoss.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`
- `README.md`
- `CHANGELOG.md`
- `Docs/Quick_Start.md`
- `Docs/User_Guide.md`


### Changed in Pit Fuel Control ModeCycle AUTO->OFF follow-up
- `PitFuelControlEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR #577 review follow-up (AUTO->OFF baseline clear)
- `PitFuelControlEngine.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control follow-up (zero transport + AUTO ownership cancel + OFF/STBY reset + offline suppression)
- `PitCommandEngine.cs`
- `PitFuelControlEngine.cs`
- `LalaLaunch.cs`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Pit_Assist.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control polish (feedback-only max-fill wording)
- `PitFuelControlEngine.cs`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control v2 redesign (AUTO-owned + MFD-derived OFF/MAN + edge-trigger cancel)
- `PitFuelControlEngine.cs`
- `LalaLaunch.cs`
- `PitCommandEngine.cs`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in class-best/class-leader live-session seam restore + single-class fallback task
- `LalaLaunch.cs`
- `Docs/Subsystems/H2H.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR #582 review follow-up (explicit single-class authority requirement)
- `LalaLaunch.cs`
- `Docs/Subsystems/H2H.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef profile-track UI integration + H2H unresolved log spam + wet/dry profile-best routing fix
- `ProfilesManagerView.xaml`
- `ProfilesManagerViewModel.cs`
- `CarProfiles.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef player-side seam reuse refactor
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in LapRef analysis-first SessionBest authority + rollover parity follow-up
- `LapReferenceEngine.cs`
- `LalaLaunch.cs`
- `Docs/Subsystems/LapRef.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in PR follow-up: PB readback wet/dry fallback on unvalidated best-lap events
- `LalaLaunch.cs`
- `Docs/Subsystems/Profiles_And_PB.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

## Delivery status highlights
- Kept changes bounded to existing runtime seams and exports: corrected projection behavior at the shared timed-race authority seam rather than introducing parallel dash property families.
- Aligned Pit Fuel Control source targets with non-clamped requirements for live modes and retained planner-owned PLAN validity/authority.
- Kept ownership boundaries intact: dashboards remain presentation/control surfaces while plugin-owned actions own pit command dispatch.
- Preserved focused helper ownership (`PitCommandEngine`) for transport/mapping/feedback/failure logic instead of widening central runtime loops.
- Added bounded observability and short-lived user feedback exports so command success/failure is visible on dash and in SimHub logs.
- Extended focused-helper ownership with `PitFuelControlEngine` for pit fuel source/mode state and decision logic while keeping `LalaLaunch` as action/export wiring.
- Kept scope bounded to pit action surface/feedback seam extensions (no dashboard JSON/UI expansion): direct source-select + zero-fuel actions, selection-only source/mode feedback publication, and max-fuel toggle-state export.

## Validation note
- Validation recorded against `HEAD` (`Class-best blank-class fallback now requires explicit native single-class authority; unknown class-count state remains fail-safe non-single-class to prevent multiclass cross-class session-best collapse during metadata startup.`).

### Changed in plugin-owned tyre control v1 task
- `PitTyreControlEngine.cs`
- `LalaLaunch.cs`
- `LaunchPlugin.csproj`
- `GlobalSettingsView.xaml`
- `Docs/Pit_Assist.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`

### Changed in Pit Fuel Control testing/polish pass (payload/observability/table alignment)
- `PitFuelControlEngine.cs`
- `PitCommandEngine.cs`
- `Docs/Subsystems/FuelModesLogicCSV.csv`
- `Docs/Internal/SimHubLogMessages.md`
- `Docs/Internal/Development_Changelog.md`
- `Docs/RepoStatus.md`
- `CHANGELOG.md`
- 2026-04-30 League Race Phase 1 UI usability follow-up landed:

  - follow-up fix: Pit command transport row visibility no longer depends on League Class CSV mode visibility; control is now always visible within Pit Commands settings.
  - player override invalid-manual persisted states now normalize to Auto-detect (manual remains active only when explicitly selected and valid);
  - League Class settings now show explicit column headers for player override and suffix fallback rule rows;
  - CSV-mode UI now displays detected class rows (read-only Phase 1 visibility table with class/short/rank/colour preview);
  - mode-driven section visibility now matches classifier mode (`CSV only`, `Name suffix only`, `CSV then name suffix`);
  - player preview now reports clear identity-unavailable text and, when available, shows player/source/resolved-class summary.
### Changed in TrackLearning dash export/action family for Offline Testing lock/review
- Added a narrow plugin-owned `TrackLearning.*` export family for dash-consumer review of current profile/track learning state:
  - `TrackLearning.PitLoss.Display/ValueSec/SourceText/Locked`,
  - `TrackLearning.Markers.EntryPct/ExitPct/Locked`,
  - `TrackLearning.Condition.IsWet/ModeText/AvgLapDisplay/AvgLapSamples/FuelSave/FuelAvg/FuelMax/FuelSamples/Locked`.
- Added lock toggle actions only:
  - `TrackLearning.PitLoss.ToggleLock`,
  - `TrackLearning.Markers.ToggleLock`,
  - `TrackLearning.Condition.ToggleLock`.
- Preserved compatibility + invariants:
  - existing `TrackMarkersLock` / `TrackMarkersUnlock` remain unchanged,
  - no relearn/reset/refresh/save actions introduced for this dash page,
  - no fuel-learning, pace-learning, pit-loss-calculation, marker-calculation, or profile-schema behavior changes.
- Lock/persistence semantics:
  - `TrackLearning.Condition.Locked` is active-condition mapped (`DryConditionsLocked` vs `WetConditionsLocked`) and therefore shared by condition avg-lap + fuel persistence ownership,
  - pit-loss and condition toggles use existing immediate profile-save behavior,
  - marker toggle uses the existing marker-store lock seam.

- 2026-05-06 H2HTrack selected-target League identity handoff fix landed:
  - `BuildH2HTrackSelector(...)` now forwards selected-slot `UserID` into H2H target selector identity so `H2HTrack.Ahead/Behind.ClassColor` and `ClassColorHex` resolve through CSV-first League Class semantics when available;
  - fallback behavior unchanged when League Class is disabled/unresolved, and no H2HTrack physical target-selection or sector/delta/timing ownership was changed.

- 2026-05-15: Strategy race-basis owner mode refresh landed; Refresh Calcs now recompute-only; preset modified indicator excludes PreRace-only differences.

- 2026-05-16: Strategy owner-vs-effective race-basis semantics tightened; dirty-state and effective visibility now derive from effective basis helpers.

- Final PR #723 cleanup: effective race-basis notification/invalidation now centralised in FuelCalcs; preset removal while Preset owner active now clears stale strategy outputs immediately.

- 2026-05-16: Strategy PreRace default mode changed from Multi Stop to Single Stop for new/default planner state and new preset template creation.
  - existing saved profile/preset PreRaceMode values are preserved;
  - legacy persisted Auto (`3`) normalization remains unchanged (`3 -> Multi Stop`);
  - no planner math, race-basis ownership, Live Detect, runtime fuel model, or dash export behavior changes.
  - follow-up fix: profile/car-load reset path now preserves loaded persisted PreRace mode (No Stop/Single Stop/Multi Stop) instead of overwriting to default Single Stop on car change.

- 2026-05-17: Strategy planner microcopy/docs alignment pass landed (wording-only, no behavior changes).
  - Refresh Calcs tooltip now states recompute-only semantics from current selected/effective inputs and explicitly says it does not reload profile/live, reapply presets, or change ownership.
  - Race Basis microcopy now consistently frames owner semantics (Preset/Lap-Limited/Time-Limited/Live Detect) and clarifies Live Detect is non-destructive to saved preset/profile state.
  - Preset reapply (`↻`) tooltip now explicitly describes deliberate preset-value reapply action.
  - Docs now clarify modified-badge semantics: calc-affecting preset divergence only, PreRace-only differences excluded, and Live Detect/manual owner divergence does not imply preset overwrite.
- 2026-05-18: DATA LIVE BUILD/SIMH fallback authority audit validated (runtime BUILD no longer uses planner default burn; SIMH burn fallback reachable and correctly surfaced as SIMH authority when profile burn is unavailable).
- 2026-05-18: PR #733 final wording cleanup: active transitional DATA label renamed BUILD -> PEND (code `Pit.FuelControl.Data=1` unchanged); terminology-only update with no runtime authority/maths/send behavior change.
- 2026-05-18: PR #733 SIM provenance guard follow-up: burn `SIM`/`SIMH` now requires genuine DataCore computed fallback provenance; synthetic/plugin fallback now reports `DEFAULT`/`DFALT` (no authority-chain or refuel-math change).
- 2026-05-18: PR #733 runtime refuel provenance propagation follow-up: runtime refuel basis now preserves genuine DataCore fallback provenance (`SIM`/`SIMH`) while plugin-held synthetic fallback remains `DEFAULT`/`DFALT` (no authority-order or math/send behavior change).
- 2026-05-18: PR #733 same-tick provenance alignment follow-up: `Pit.FuelControl.DataText` now shares runtime refuel fallback provenance, eliminating SIM/DFALT contradiction on the same tick (no authority-order or math/send behavior change).
- 2026-05-18: Property Snapshot PER LAP compile wiring fix validated.
  - `DataUpdate(...)` now runs Property Snapshot manual-marker + FREQUENCY automation only (session-time in scope).
  - PER LAP automation is now triggered from `UpdateLiveFuelCalcs(...)` at the existing `DetectLapCrossing(...)` seam where `lapCrossed` + `CompletedLaps` context is valid.
  - Behavior invariants preserved: manual marker still writes one-shot (+optional rolling), automation remains rolling-only, PER LAP remains one capture per completed lap.
- 2026-05-18: Strategy Profile-mode fuel preview now live-refreshes after telemetry fuel persistence for the active track/condition (AVG/ECO/MAX labels + button availability refresh without mode/track toggle; profile-avg textbox auto-apply remains source-ownership guarded).
- 2026-05-19: PR #744 follow-up landed: Strategy Profile telemetry-refresh now re-applies active Profile fuel choice (AVG/ECO/MAX), covers cold no-profile first-learn auto-apply, and updates wet derived-from-dry fallback paths without DATA/Fuel.Refuel/Pit-control contract changes.
- 2026-05-19: PR #744 follow-up #2 landed: telemetry profile-fuel refresh now requires selected Strategy profile+track identity match and preserves non-manual source tracking when auto-applying Profile AVG/ECO/MAX.
- 2026-05-19: PR #744 follow-up #3 landed: Profile AVG/ECO/MAX button handlers now use source-safe non-manual apply path, preserving telemetry-refresh eligibility for active Profile choice.
- 2026-05-19: PR #744 follow-up #4 landed: wet derived-from-dry relevance now applies per active Profile choice (AVG/ECO/MAX) and profile fuel text sync no longer rounds numeric `FuelPerLap` through textbox parse-back.

- 2026-05-28: Fuel Revamp Phase 3C Export Rationalisation design pass completed as analysis-only.
  - mapped target fuel/pit/strategy/projection export families against internal C# defining/consumer seams and documented dashboard-consumer status;
  - confirmed no export removal/rename, no runtime calculation change, no dashboard JSON/package change, and no compatibility cleanup performed;
  - no Property Snapshot grouping update required in this phase (no export-surface change).

- 2026-05-28: Fuel Revamp Phase 3E Dashboard Migration Design pass completed (design-only).
  - documented mirror/smoothing consumer migration strategy and explicit no-change decisions for stable/source/confidence helpers;
  - no export removed/renamed; no C# or dashboard `.djson` changes performed in this phase;
  - implementation checklist/validation matrix prepared for future dashboard migration task once `.djson` package files are available in-repo for direct edits.


- 2026-05-28 Strategy Live Detect default + non-destructive preset apply landed:
  - Strategy now prefers Live Detect as startup/default race-basis owner once live session context is active only while the owner is untouched; explicit Preset/Lap/Time selections and manual Race Minutes/Laps edits are preserved.
  - Race Preset selector/reapply controls are visible only in Preset or Live Detect owner modes (not manual Lap/Time), and remain available in Live Detect.
  - Selecting/reapplying/updating applied presets while Live Detect is active applies setup values without stealing race-basis ownership or clearing live-detect helper/cache state.
