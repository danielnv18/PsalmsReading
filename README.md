# PsalmsReading (planning state)

This repo will host a .NET 10 solution for scheduling and tracking Sunday psalm readings, with a Blazor WASM frontend (`PsalmsReading.UI`) and a minimal API backend. Data comes from `psalms_full_list.csv` (Spanish), while code uses English identifiers.

## How it will run (once scaffolded)
1) Restore and build: `dotnet restore` then `dotnet build`.
2) Apply migrations/seed (once implemented): run the API project, which will seed Sqlite from `psalms_full_list.csv` on first launch.
3) Start backend: `dotnet run --project PsalmsReading.Api`.
4) Start frontend: `dotnet run --project PsalmsReading.UI` (served via WASM, calling the API).

## Current status
- Projects not yet scaffolded; see `PLAN.md` for the architecture and CLI steps to create them.
- Solution file in use: `PsalmsReading.slnx` (no `.sln`).

## Next steps (summary)
- Scaffold projects via dotnet CLI per `PLAN.md`.
- Add EF Core Sqlite, CSV import, scheduling logic, ICS export, and Blazor pages.
