# PsalmsReading Plan

## Planned solution layout (net10.0)
- PsalmsReading.Domain — entities/value objects/events (English identifiers, Spanish data values).
- PsalmsReading.Application — use cases, service interfaces, DTOs.
- PsalmsReading.Infrastructure — EF Core Sqlite, CSV import, scheduling logic, ICS export.
- PsalmsReading.Api — minimal API.
- PsalmsReading.UI — Blazor WASM UI (calls API).
- PsalmsReading.Tests — xUnit tests.

## Data and rules
- Import psalms from `psalms_full_list.csv` on first run to seed the database; all reads thereafter come from the database (code in English identifiers, data in Spanish; use `Type`, not `Tipo`).
- Scheduling (for 1/2/3/6 months on request):
  1) Only psalms with `total_verses <= 30`.
  2) Prefer least-read psalms.
  3) First Sunday each month: `Type` = alabanza (fallback to temas if needed).
  4) December: prefer mesiánico (fallback via temas).
  5) Sunday before/during/after Holy Week: choose among 113–118 (incl. 117) with fewest reads.
  6) If no match on `Type`, search `Themes`.
  7) If no special rule, pick general least-read.
  8) Always exclude 35, 55, 59, 69, 79, 109, 137.
- Holy Week inferred from Easter; calendar timezone: local.
- Reading record captures only the date.
- ICS body template:
  ```
  Salmo X - Titulo
  Categoria: X
  Link: https://www.bible.com/bible/103/PSA.27.NBLA
  ```

## Phased plan (dotnet CLI only)

### Phase 1 — Skeleton and projects
1) Create projects and add to `PsalmsReading.slnx`:
   - `PsalmsReading.Domain` (classlib)
   - `PsalmsReading.Application` (classlib)
   - `PsalmsReading.Infrastructure` (classlib)
   - `PsalmsReading.Api` (webapi)
   - `PsalmsReading.UI` (blazorwasm)
   - `PsalmsReading.Tests` (xunit)
2) Wire references:
   - Application -> Domain
   - Infrastructure -> Domain, Application
   - Api -> Application, Infrastructure
   - UI -> Api (HTTP client/shared DTOs)
   - Tests -> Domain/Application (plus others as needed)
3) Build once to confirm structure: `dotnet build`.

### Phase 2 — Domain/Application contracts
1) Define Domain entities/value objects (Psalm, ReadingRecord, PlannedReading).
2) Define Application interfaces and DTOs (repositories, scheduling service, calendar exporter).
3) Add unit tests for basic domain rules (xUnit).

### Phase 3 — Infrastructure data layer
1) Add packages: `Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `CsvHelper`.
2) Implement DbContext, configurations, migrations.
3) Implement CSV import on first run to seed the database.
4) Add repository implementations.
5) `dotnet ef migrations add InitialCreate` then `dotnet ef database update`.

### Phase 4 — Scheduling and calendar
1) Add package: `Ical.Net`.
2) Implement scheduling service with the rules listed above (Easter/Holy Week calculation).
3) Implement ICS exporter with the provided template.
4) Add tests for scheduling/ICS.

### Phase 5 — API
1) Add package: `Swashbuckle.AspNetCore`.
2) Minimal API endpoints: psalms, readings, schedule generation/export.
3) Hook up DI for Infrastructure services.
4) `dotnet run --project PsalmsReading.Api` to verify.

### Phase 6 — UI
1) Blazor WASM pages: psalm catalog, reading history (add date), scheduler (1/2/3/6 months, download ICS).
2) Wire HTTP client to API endpoints.
3) `dotnet run --project PsalmsReading.UI` to verify.

### Phase 7 — Polish
1) `dotnet format`, `dotnet build`, `dotnet test`.
2) Add README updates/screenshots if needed.
