# PsalmsReading

.NET 10 clean-architecture solution for scheduling and tracking Sunday psalm readings. Data stays in Spanish (from `psalms_full_list.csv`); code uses English identifiers. Frontend: Blazor WASM (`PsalmsReading.UI`). Backend: minimal API (`PsalmsReading.Api`) with Sqlite.

## Run locally
- Restore/build: `dotnet restore` then `dotnet build`.
- Start API (seeds DB on first run from `psalms_full_list.csv`): `dotnet run --project PsalmsReading.Api` (launch settings: http://localhost:5158, https://localhost:7158).
- Start UI: `dotnet run --project PsalmsReading.UI` (launch settings: http://localhost:5174, https://localhost:7174). UI calls the API base URL from `PsalmsReading.UI/wwwroot/appsettings.json` (default `http://localhost:5158/api/`).
- Swagger/OpenAPI: browse `http://localhost:5158/swagger` (or `/openapi/v1.json`).

## Re-import psalm data
- Edit `psalms_full_list.csv` in the repo root.
- With the API running, call `POST /api/psalms/reimport` to replace psalm/theme/epigraph data from the CSV. Reading history and planned readings are left intact.
- CLI (without starting the web host): `dotnet run --project PsalmsReading.Api -- --reimport` (reads the CSV and exits).

## Scheduling logic (where and what)
- Implementation: `PsalmsReading.Infrastructure/Services/ReadingScheduler.cs`.
- Rules: only psalms with `TotalVerses <= 30`; exclude 35, 55, 59, 69, 79, 109, 137; prioritize least-read; first Sunday of the year prefers theme `Días festivos: año nuevo`; Holy Week Sundays pick among 113–118 (fewest reads); December prefers mesiánico (type, then theme, then epigraphs); first Sunday of each month prefers alabanza (type then theme); otherwise least-read. No duplicates per generated schedule. Easter is inferred per year. ICS export is in `PsalmsReading.Infrastructure/Services/CalendarExporter.cs` (uses the provided body template and Bible.com link).

## Project structure (solution `PsalmsReading.slnx`)
- `PsalmsReading.Domain`: entities (Psalm, ReadingRecord, PlannedReading) with English identifiers.
- `PsalmsReading.Application`: interfaces for repositories, scheduling, calendar export, import.
- `PsalmsReading.Infrastructure`: EF Core Sqlite `PsalmsDbContext`, repositories, CSV import, scheduling engine, ICS exporter.
- `PsalmsReading.Api`: minimal API, DI wiring, seeding/reimport endpoint, Swagger.
- `PsalmsReading.UI`: Blazor WASM UI (catalog, reading history CRUD, scheduler preview/save/ICS).
- `PsalmsReading.Tests`: xUnit tests (domain, scheduling, import/ICS).
