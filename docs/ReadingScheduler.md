## ReadingScheduler rules

The scheduler generates Sunday readings by running a small pipeline of rule objects (in priority order). Every rule decides if it can handle the given Sunday and, if so, tries to pick a psalm. If it cannot pick one, the scheduler continues to the next rule. The last rule (`General`) always runs and guarantees the standard selection.

### Current rule order

1. `First Sunday new year` — looks for theme `Días festivos: año nuevo`.
2. `Thanksgiving` — last two Sundays of November; looks for theme `Días festivos: Agradecimiento`.
3. `HolyWeek` — Palm Sunday, Easter Sunday, and the Sunday after; prefers psalms 113–118.
4. `Christmas season` — Sundays in December; looks for type/theme/epigraph `mesiánico`.
5. `First Sunday of worship` — first Sunday of any other month; looks for type/theme/epigraph `alabanza`.
6. `General` — balances types using coverage plus recent usage (favoring under-read and recently under-used types), then picks least-read psalms (random tie-breaks for large tiers). It also enforces a monthly cap and nudges in types that haven't appeared this month when they still have many unread psalms.

### How selection works

- Psalms with more than 30 verses or in the excluded list (35, 55, 59, 69, 79, 109, 137) are skipped.
- Read counts come from `IReadingRepository` and drive the tiered selection (least read first; if a tier has 1–2 options, take the first; if more than 2, pick randomly).
- Type balancing uses coverage per type (distinct psalms read at least once vs total readable per type).
- No more than two of the same type are allowed in a row.
- For `General`, recent usage within the rolling 6-week window reduces a type's priority but does not hard-exclude it.
- `General` avoids picking any type that already appears twice in the current calendar month. If a type has not appeared in the month and still has at least ~50% of its readable psalms unread, it is preferred.
- Existing planned readings are included when calculating streaks and rolling type counts.
- Theme/type/epigraph matches are normalized (case/accent-insensitive, trimmed).

### Updating a rule

1. Open `PsalmsReading.Infrastructure/Services/ReadingRules/`.
2. Locate the rule class (e.g., `DecemberRule`) that encapsulates the behavior you want to change.
3. Adjust its `CanApply` condition or `Select` method. Keep the `Name` consistent if external callers or tests rely on it.

### Adding a new rule

1. Add a new class implementing `IReadingRule` in `PsalmsReading.Infrastructure/Services/ReadingRules/`.
2. Implement:
   - `Name` — the value stored in `PlannedReading.RuleApplied`.
   - `CanApply(ScheduleContext context)` — returns `true` when the rule should run.
   - `Select(ScheduleContext context)` — returns a `Psalm` or `null` to fall through.
3. Register the rule in `_rules` in the desired priority position (earlier means higher priority).
4. Add/adjust unit tests in `PsalmsReading.Tests/SchedulingTests.cs` to cover the new behavior.
