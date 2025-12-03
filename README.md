# PsalmsReading (planning state)

This repo will host a .NET 10 solution for scheduling and tracking Sunday psalm readings, with a Blazor WASM frontend (`PsalmsReading.UI`) and a minimal API backend. Data comes from `psalms_full_list.csv` (Spanish), while code uses English identifiers.

## How it will run (once scaffolded)
1) Restore and build: `dotnet restore` then `dotnet build`.
2) Apply migrations/seed (once implemented): run the API project, which will seed Sqlite from `psalms_full_list.csv` on first launch.
3) Start backend: `dotnet run --project PsalmsReading.Api`.
4) Start frontend: `dotnet run --project PsalmsReading.UI` (served via WASM, calling the API).

## Current status
- Projects scaffolded and added to `PsalmsReading.slnx`: Domain, Application, Infrastructure, Api, UI (Blazor WASM), Tests (xUnit). Domain/Application contracts and initial domain tests are in place. Infrastructure has EF Core setup with Sqlite, normalized themes/epigraphs tables, repositories, a CSV import service, API wiring, and an initial migration.
- Solution file in use: `PsalmsReading.slnx` (no `.sln`).

## Next steps (summary)
- Implement scheduling logic + ICS export; expose minimal API endpoints; build Blazor UI pages; finish with format/build/test.
