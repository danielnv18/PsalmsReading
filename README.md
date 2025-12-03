# PsalmsReading (planning state)

This repo will host a .NET 10 solution for scheduling and tracking Sunday psalm readings, with a Blazor WASM frontend (`PsalmsReading.UI`) and a minimal API backend. Data comes from `psalms_full_list.csv` (Spanish), while code uses English identifiers.

## How it will run (once scaffolded)
1) Restore and build: `dotnet restore` then `dotnet build`.
2) Apply migrations/seed (once implemented): run the API project, which will seed Sqlite from `psalms_full_list.csv` on first launch.
3) Start backend: `dotnet run --project PsalmsReading.Api`.
4) Start frontend: `dotnet run --project PsalmsReading.UI` (served via WASM, calling the API).

## Current status
- Projects scaffolded and added to `PsalmsReading.slnx`: Domain, Application, Infrastructure, Api, UI (Blazor WASM), Tests (xUnit). Infrastructure has EF Core setup with Sqlite, normalized themes/epigraphs tables, repositories, CSV import service, API wiring, initial migration applied, scheduling service, and ICS exporter. Unit tests cover domain helpers and scheduling rules.
- Solution file in use: `PsalmsReading.slnx` (no `.sln`).

## Next steps (summary)
- Add the `Ical.Net` package via CLI: `dotnet add PsalmsReading.Infrastructure package Ical.Net`.
- Expose scheduling/calendar via minimal API endpoints; build Blazor UI pages; finish with format/build/test.

## Scheduling logic (location)
- Engine: `PsalmsReading.Infrastructure/Services/ReadingScheduler.cs` implements the rules.
- Rules applied in order: (1) only psalms with `TotalVerses <= 30`; (2) exclude 35, 55, 59, 69, 79, 109, 137; (3) prefer least-read; (4) Palm/Easter/after Easter Sundays pick among 113–118 (fewest reads); (5) December prefers `mesiánico` (type, then theme fallback); (6) first Sunday of each month prefers `alabanza` (type, then theme fallback); (7) otherwise general least-read; no duplicates in a generated schedule.
- Easter/Holy Week: inferred per year (Palm = Easter - 7 days; Sunday after Easter = +7 days).
- ICS export: `PsalmsReading.Infrastructure/Services/CalendarExporter.cs` builds events for planned readings using the required body template and Bible.com link.
