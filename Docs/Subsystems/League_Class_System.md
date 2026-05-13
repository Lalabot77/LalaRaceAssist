# League Class System

## 1) Purpose
League Class is a **presentation and cohorting layer** for league-specific subclass behavior. It does not replace physical-awareness selectors (CarSA slot selection, H2HTrack nearest-target selection, Opponents race-order ownership). It enables league-defined classes that can differ from native iRacing class labels/colors.

## 2) Core concepts
- **Native class:** iRacing-provided class identity/presentation.
- **Effective class:** resolver output used for League-aware cohort/presentation when League Class is enabled and resolved.
- **CSV mapping:** `CustomerId/UserID -> League class name` mapping source.
- **Suffix fallback:** name/suffix-based fallback path when mode allows and CSV is unavailable/unresolved.
- **Manual player override:** player-only forced effective class override.
- **Effective cohort:** all drivers matching the same effective class for race-context cohort semantics.
- **Effective PositionInClass:** published class position inside effective cohort (with native fallback when unresolved).

## 3) Resolver precedence and fallback
Resolver source is exported as `LeagueClass.Player.Source` (`CSV`, `NAME`, `MANUAL`, `NATIVE`, `NONE`).

### Active precedence
1. **Manual player override** (player row only) when enabled/valid.
2. **CSV mapping** when row matches identity and class definition is valid/enabled.
3. **Suffix/name fallback** when mode permits and CSV path is missing/unresolved.
4. **Native fallback** when League Class is disabled, unresolved, or invalid.

### Mode examples
- **CSV only:** uses CSV rows; unresolved rows fall back native.
- **Suffix only:** uses suffix/name classifier; unresolved rows fall back native.
- **CSV then suffix:** CSV first, suffix/name secondary fallback.
- **Disabled:** all class presentation/cohort behavior is native.

## 4) Runtime ownership boundaries
League Class does **not** change selector ownership:
- CarSA slot selection/order/filtering unchanged.
- H2HTrack target selection unchanged (CarSA-owned).
- H2HRace selector logic unchanged (Opponents-owned race target seam).
- Opponents race-order logic unchanged.

League Class only changes **presentation/cohort semantics** at publish/race-context seams.

## 5) League-aware exports
Major affected families:
- `Car.Player.*`
- `Car.Ahead01..05.*` / `Car.Behind01..05.*`
- `H2HRace.*`
- `H2HTrack.*` (presentation fields while keeping CarSA target ownership)
- `Opp.Ahead1..5.*` / `Opp.Behind1..5.*`
- `LeagueClass.Player.*`
- `PitExit.*` class-facing fields (`Ahead/Behind.ClassColor`, class-cohort prediction fields)

Contract notes:
- `ClassName` presentation prefers effective League `ShortName`, then effective League `Name`.
- `ClassColor` and `ClassColorHex` keep family formatting contracts (`0xRRGGBB` vs `#RRGGBB`) per export family.
- Disabled/unresolved paths keep native class presentation.
- `Car.Player.PositionInClass` and `PitExit.PredictedPositionInClass` follow effective-class cohorts only when League Class is enabled+resolved, otherwise native fallback.

## 6) Effective PositionInClass semantics
- Each published row shows position inside that driver’s **effective cohort** when effective context is available.
- If effective context is unavailable/unresolved, publication falls back to native class position.
- Opponents owns effective race-order seam used by published effective-position contexts; CarSA/H2H consume that seam without taking ownership.

## 7) UI workflow (League Race settings)
- CSV load path + reload flow.
- Compact CSV status line (`Status | Rows | Valid | Invalid | Duplicates`).
- Warning-state helper text for CSV/mapping issues.
- Auto-detect preview for player effective class.
- Manual override editing for player class only.
- `LeagueClass.ToggleEnabled` toggle behavior with enable guard.
- Quiet enable self-check reload in CSV-capable modes when file exists but valid rows are not yet loaded.
- Healthy state: enabled + resolved effective class (or clean native fallback when disabled).
- Unhealthy state: missing/invalid CSV rows, unresolved effective class while enabled, invalid class definition wiring.

## 8) Dash integration guidance
- Dashboards should consume plugin-owned exports directly.
- Dashboards should not re-implement League resolution in NCALC/JS.
- Use published `ClassName`/`ClassColor`/`ClassColorHex` and published `PositionInClass` directly.
- Expect native fallback when League Class is disabled/unresolved.

## 9) Limitations / non-goals
- Does not alter official race results.
- Does not alter native iRacing classing.
- Manual override is player-only.
- Does not globally force every driver into League mapping when unresolved.
- Unresolved drivers can remain native.

## 10) Troubleshooting
- **CSV loaded but driver unresolved:** check identity keys (`UserID`/name path), mode, and class-definition validity.
- **Missing ShortName:** `ClassName` falls back to effective League `Name`.
- **Duplicate rows:** duplicates are counted/reportable; resolve CSV hygiene to avoid ambiguity.
- **Missing UserID:** CSV identity may fail; suffix fallback may apply only when mode allows.
- **Replay/session mismatch:** cohort identity can differ from live due to replay/session info availability.
- **Suffix fallback expectation mismatch:** verify mode is suffix-capable and classifier inputs match expected naming conventions.

## Integration references
- `Docs/Subsystems/CarSA.md`
- `Docs/Subsystems/Opponents.md`
- `Docs/Subsystems/H2H.md`
- `Docs/Subsystems/Dash_Integration.md`
- `Docs/Internal/SimHubParameterInventory.md`
