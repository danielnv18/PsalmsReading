## ReadingScheduler rules

The scheduler generates Sunday readings by running a small pipeline of rule objects (in priority order). Every rule decides if it can handle the given Sunday and, if so, tries to pick a psalm. If it cannot pick one, the scheduler continues to the next rule. The last rule (`General`) always runs and guarantees the standard selection.

### Current rule order

1. `First Sunday new year` — looks for theme `Días festivos: año nuevo`.
2. `Thanksgiving` — last two Sundays of November; looks for theme `Días festivos: Agradecimiento`.
3. `HolyWeek` — Palm Sunday, Easter Sunday, and the Sunday after; prefers psalms 113–118.
4. `Christmas season` — Sundays in December; looks for type/theme/epigraph `mesiánico`.
5. `First Sunday of worship` — first Sunday of any other month; looks for type/theme/epigraph `alabanza`.
6. `General` — least-read psalms, breaking ties with light randomness when tiers have more than two options.

### How selection works

- Psalms with more than 30 verses or in the excluded list (35, 55, 59, 69, 79, 109, 137) are skipped.
- Read counts come from `IReadingRepository` and drive the tiered selection (least read first; if a tier has 1–2 options, take the first; if more than 2, pick randomly).
- Theme/type/epigraph matches are normalized (case/accent-insensitive, trimmed).

### Updating a rule

1. Open `PsalmsReading.Infrastructure/Services/ReadingScheduler.cs`.
2. Locate the rule class (e.g., `DecemberRule`) that encapsulates the behavior you want to change.
3. Adjust its `CanApply` condition or `Select` method. Keep the `Name` consistent if external callers or tests rely on it.

### Adding a new rule

1. Add a new class implementing `IReadingRule` inside `ReadingScheduler.cs` (or move rules to their own file if they grow).
2. Implement:
   - `Name` — the value stored in `PlannedReading.RuleApplied`.
   - `CanApply(ScheduleContext context)` — returns `true` when the rule should run.
   - `Select(ScheduleContext context)` — returns a `Psalm` or `null` to fall through.
3. Register the rule in `_rules` in the desired priority position (earlier means higher priority).
4. Add/adjust unit tests in `PsalmsReading.Tests/SchedulingTests.cs` to cover the new behavior.
