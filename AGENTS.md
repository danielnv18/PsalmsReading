# Repository Guidelines

## Project Structure & Module Organization
- Current root files: `PsalmsReading.slnx`, `PLAN.md`, `README.md`, `psalms_full_list.csv`.
- Planned projects (see `PLAN.md`): `PsalmsReading.Domain`, `PsalmsReading.Application`, `PsalmsReading.Infrastructure`, `PsalmsReading.Api`, `PsalmsReading.UI` (Blazor WASM), `PsalmsReading.Tests` (xUnit). All will be managed through the solution file and follow clean architecture boundaries.
- Data: `psalms_full_list.csv` seeds the database on first run; afterward, all reads/writes go through the database. Themes and epigraphs are normalized (lookup tables + join tables).

## Build, Test, and Development Commands
- Restore/build solution: `dotnet restore` then `dotnet build`.
- Run API: `dotnet run --project PsalmsReading.Api` (seeds DB from `psalms_full_list.csv` on first run if empty).
- Run UI: `dotnet run --project PsalmsReading.UI`.
- Tests: `dotnet test`.
- Format: `dotnet format`.
- Migrations: add with `dotnet ef migrations add <Name> -p PsalmsReading.Infrastructure -s PsalmsReading.Api --output-dir Migrations`; apply with `dotnet ef database update -p PsalmsReading.Infrastructure -s PsalmsReading.Api`.

## Coding Style & Naming Conventions
- Language: C# 10+ (`net10.0`), clean architecture. Code identifiers in English; data values in Spanish. Use `Type` instead of `Tipo`, etc.
- Formatting: follow `.editorconfig` (spaces, size 4 for code; size 2 for JSON/YAML/XML). Keep files ASCII/UTF-8-BOM.
- Project naming: `PsalmsReading.*` per layer; avoid introducing new prefixes.
- Avoid hardcoding paths; prefer configuration and DI.

## Testing Guidelines
- Framework: xUnit with FluentAssertions (planned). Place tests in `PsalmsReading.Tests`.
- Naming: one test class per SUT; method names `Method_Should_Behavior`.
- Coverage focus: scheduling logic, ICS exporter, CSV import/seed, EF repositories.
- Run all tests before PR: `dotnet test`.

## Commit & Pull Request Guidelines
- Commits: concise, imperative mood (e.g., “Add scheduling service”), group related changes.
- PRs: summarize scope, reference issues, note breaking changes, and include test results (`dotnet test` output). Add screenshots/GIFs for UI changes when available.

## Architecture Overview
- Layers: Domain (entities/value objects), Application (use cases/contracts), Infrastructure (EF Core Sqlite, CSV import, scheduling, ICS export), Api (minimal API), UI (Blazor WASM).
- Scheduling rules: max 30 verses; prioritize least-read; first-Sunday alabanza; December mesiánico; Holy Week uses 113–118; fallback to themes; exclude 35, 55, 59, 69, 79, 109, 137. Holy Week inferred from Easter. ICS body template in `PLAN.md`.
