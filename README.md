# Prepapp

A personal interview-prep app for intermediate .NET roles: a question bank with spaced-repetition drills, and (phase 2) an AI mock interviewer that asks, follows up and grades against a rubric. The full spec is in [docs/spec.md](docs/spec.md).

## Status

Phase 1 (MVP) is in place:

- **Question bank**: questions tagged by category, topic and difficulty, with key points and a model answer. Add them in the UI or import them as JSON.
- **Flashcard drill**: reveal the answer, then rate it Again / Hard / Good / Easy. SM-2 schedules the next review.
- **Dashboard**: due count and accuracy per category, with the weakest topics first.

## Stack

.NET 10 · Blazor Web App (interactive server) · Minimal APIs · EF Core · SQLite (local) · xUnit + FluentAssertions

```
src/
  Prepapp.Domain           Entities and SM-2 scheduling; no dependencies
  Prepapp.Infrastructure   EF Core DbContext and migrations
  Prepapp.Web              Blazor UI and /api endpoints, organised by feature (Features/*)
tests/
  Prepapp.UnitTests        Domain logic (SM-2, Question invariants)
  Prepapp.IntegrationTests WebApplicationFactory against a throwaway SQLite database
```

Each feature has a service (for example `ReviewService`) that both the Blazor pages and the minimal-API endpoints call.

## Run

```powershell
dotnet run --project src/Prepapp.Web    # http://localhost:5047
dotnet test
```

On startup the app applies migrations. If the question bank is empty, it imports `src/Prepapp.Web/SeedData/questions.json`.

### Migrations

```powershell
dotnet tool restore
dotnet ef migrations add <Name> --project src/Prepapp.Infrastructure --startup-project src/Prepapp.Web --output-dir Data/Migrations
```

## API

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/questions?category=&difficulty=&topic=` | List and filter questions (`topic` matches a substring) |
| GET | `/api/questions/{id}` | One question |
| POST | `/api/questions` | Add a question |
| POST | `/api/questions/import` | Bulk import from a JSON array; skips prompts already in the bank |
| GET | `/api/review/due?limit=20&category=` | Cards due for review |
| POST | `/api/review/{cardId}/rate` | `{ "rating": "Again" \| "Hard" \| "Good" \| "Easy" }` |
| GET | `/api/stats/overview` | Totals, accuracy per category, weakest topics |

## Not yet done

- Auth (ASP.NET Core Identity, single user). Every endpoint is open, so only run it locally for now.
- Postgres provider and Testcontainers integration tests (needs Docker).
- Phase 2: AI mock interviewer, rubric feedback and the STAR story bank.
