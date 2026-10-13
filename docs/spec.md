# Interview Prep App — Technical Spec

Oct 13, 2026 · @Danie

## Overview

A personal web app that drills the four things US firms test when hiring an intermediate .NET developer: C#/.NET depth, algorithms, light system design and behavioral answers. The core is an AI mock interviewer that asks, probes with follow-ups and grades against a rubric; a question bank with spaced repetition sits around it.

**Target user:** one developer (you) with ~5 years of ASP.NET experience, preparing for US mid-level roles. Multi-user support is a later option, not a design driver.

**Goals**

- Practise realistic interviews on demand, with feedback specific enough to act on.
- Track weak areas over time and resurface them until they stick.
- Double as a portfolio piece showing clean architecture, LLM integration and tests.

**Non-goals**

- Rebuilding LeetCode's judge or problem library — use LeetCode or NeetCode for algorithm reps.
- Real-time voice interviews in v1.
- Job search, CV building or application tracking.

## Scope and features

Build in three phases; phases 1 and 2 are the product, phase 3 is optional.

| Phase | Feature | What it does | Effort |
| --- | --- | --- | --- |
| 1 — MVP | Question bank | Questions tagged by category, difficulty and topic; model answers and key points | Low |
| 1 — MVP | Flashcard drill | Reveal-and-self-rate (Again / Hard / Good / Easy) feeding spaced repetition | Low |
| 1 — MVP | Progress dashboard | Accuracy and due-count per category; weakest topics first | Low |
| 2 — Core | AI mock interview | Timed session of 4–6 questions; LLM asks, follows up, then grades | Medium |
| 2 — Core | Rubric feedback | Per-answer scores on fixed criteria, strengths, gaps, a better model answer | Medium |
| 2 — Core | Behavioral story bank | Store STAR stories; AI critiques them and maps each to common prompts | Low |
| 3 — Optional | Code runner | Run C# answers against test cases in a sandbox (Judge0) | Medium |
| 3 — Optional | System design canvas | Sketch components; AI reviews the written explanation, not the drawing | Medium |
| 3 — Optional | Voice mode | Speech-to-text answers for behavioral practice | High |

**Out of scope:** an algorithm problem library, multi-tenant accounts, payments, mobile apps.

## Architecture and tech stack

One ASP.NET Core app (a modular monolith) serves the Blazor UI and the API; the only moving parts outside it are Postgres, the LLM API and, later, Judge0.

The Blazor UI talks only to the endpoints; all LLM and database access sits behind interfaces in Infrastructure, so providers can be swapped and faked in tests.

| Layer | Choice | Why |
| --- | --- | --- |
| Runtime | .NET 9 (move to .NET 10 LTS when convenient) | Current, familiar |
| UI | Blazor Web App, interactive server mode | Stay in C#; streaming chat is simple over SignalR |
| API | Minimal APIs, grouped by feature | Less ceremony; good to discuss in interviews |
| Data | EF Core + PostgreSQL (SQLite for local dev) | JSON columns for rubric scores |
| LLM | Anthropic or OpenAI SDK behind `ILlmClient` | Swap models without touching domain code |
| Code runner | Judge0 (self-hosted or hosted API) | Sandboxing is solved; don't rebuild it |
| Auth | ASP.NET Core Identity, single user | Enough for a personal tool |
| Tests | xUnit, FluentAssertions, Testcontainers | Real Postgres in integration tests |

## Data model

Seven EF Core entities cover all three phases; `Question` is the hub everything else points at.

| Entity | Key fields | Relationships |
| --- | --- | --- |
| `Question` | Id, Category (enum), Topic, Difficulty (1–3), Prompt, ModelAnswer, KeyPoints (JSON), Source | Has many `ReviewCard`, `InterviewTurn` |
| `ReviewCard` | Id, QuestionId, EaseFactor, IntervalDays, DueAt, Repetitions, LastRating | Belongs to `Question` |
| `InterviewSession` | Id, Mode (Technical / Behavioral / Mixed), TargetLevel, StartedAt, EndedAt, OverallScore, Summary | Has many `InterviewTurn` |
| `InterviewTurn` | Id, SessionId, QuestionId?, Order, Role (Interviewer / Candidate), Content, CreatedAt | Belongs to `InterviewSession`; optional `Question` |
| `AnswerEvaluation` | Id, TurnId, Scores (JSON per criterion), Strengths, Gaps, ImprovedAnswer, RubricVersion | One-to-one with a candidate `InterviewTurn` |
| `StarStory` | Id, Title, Situation, Task, Action, Result, Tags, LastCritique | Many-to-many with `Question` (behavioral prompts it answers) |
| `CodeSubmission` *(phase 3)* | Id, QuestionId, Language, SourceCode, Passed, Total, RuntimeMs, SubmittedAt | Belongs to `Question` |

**Notes**

- `Category` enum: `CSharpLanguage`, `AspNetCore`, `EfCoreData`, `Algorithms`, `SystemDesign`, `Testing`, `Behavioral`.
- Store rubric scores as JSON so criteria can change without migrations; keep `RubricVersion` to compare scores fairly over time.
- Spaced repetition uses SM-2: on each rating, update `EaseFactor` and `IntervalDays`, then set `DueAt`.

## AI mock interviewer

The interviewer is two separate LLM roles: an **Interviewer** that runs the conversation and an **Evaluator** that grades it afterwards. Splitting them keeps the interviewer from going soft and makes grades reproducible.

**Session flow**

1. User picks mode (Technical, Behavioral, Mixed), length (4–6 questions) and focus categories.
2. `QuestionSelector` picks questions: due or weak topics first, then random within focus, never repeating within 7 days.
3. Interviewer asks question 1, seeded with the question's prompt and key points (hidden from the user).
4. User answers in text; a per-question timer runs (5 min technical, 3 min behavioral).
5. Interviewer asks up to 2 follow-ups if key points are missing or the answer is vague, then moves on.
6. After the last question, Evaluator grades every answer and writes a session summary.
7. Scores below 3 on any criterion schedule that question's `ReviewCard` as due tomorrow.

**Interviewer prompt — core rules**

- You are a senior engineer at a US company interviewing for a mid-level .NET role.
- Ask one question at a time. Never reveal the expected answer or key points.
- Probe like a real interviewer: "why", "what happens when", "what would you change at 10x scale".
- Stay neutral; no praise beyond a brief acknowledgement.
- Reply in JSON: `{ "message": string, "action": "follow_up" | "next_question" }`.

**Evaluation rubric (each scored 1–5)**

| Criterion | Technical answers | Behavioral answers |
| --- | --- | --- |
| Correctness / Relevance | Facts and code are right | Story answers the question asked |
| Depth | Explains why, trade-offs, edge cases | Shows own decisions, not the team's |
| Communication | Structured, concise, uses correct terms | Clear STAR shape, under ~2 minutes |
| Practical experience | Ties to real usage, pitfalls seen | Measurable result, lesson learned |

Evaluator output is strict JSON matching `AnswerEvaluation`, validated before saving; on schema failure, retry once with the validation error appended.

**Guardrails**

- Run the Evaluator at temperature 0 and pin `RubricVersion` so scores are comparable across weeks.
- Calibrate with 10 hand-graded sample answers; adjust the prompt until the Evaluator lands within 1 point of your grades.
- Cap tokens per session (~30k) and show a running cost estimate.

## API endpoints

Minimal APIs grouped by feature; Blazor calls these through a typed client so the backend stays usable from a future mobile or CLI client.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/api/questions?category=&difficulty=&topic=` | List and filter questions |
| POST | `/api/questions` | Add a question (admin) |
| POST | `/api/questions/import` | Bulk import from JSON |
| GET | `/api/review/due?limit=20` | Cards due for spaced repetition |
| POST | `/api/review/{cardId}/rate` | Submit a rating (Again / Hard / Good / Easy) |
| POST | `/api/interviews` | Start a session; returns the first interviewer message |
| POST | `/api/interviews/{id}/turns` | Send an answer; returns the next interviewer message (streamed via SSE) |
| POST | `/api/interviews/{id}/complete` | End the session and run evaluation |
| GET | `/api/interviews/{id}` | Session transcript, scores and summary |
| GET | `/api/stats/overview` | Accuracy, streaks, weakest topics |
| GET / POST / PUT | `/api/stories` | Manage STAR stories |
| POST | `/api/stories/{id}/critique` | AI critique of one story |
| POST | `/api/code/run` *(phase 3)* | Submit C# for sandboxed test run |

## Content plan

Seed about 200 questions before phase 2; the AI interviewer is only as good as the key points it is checking against.

| Category | Seed count | Example topics |
| --- | --- | --- |
| C# language | 40 | async/await and deadlocks, `IEnumerable` vs `IQueryable`, value vs reference types, records, generics and variance, `IDisposable`, LINQ deferred execution |
| ASP.NET Core | 35 | middleware pipeline, DI lifetimes and captive dependencies, model binding and validation, auth (JWT, cookies, policies), filters, minimal APIs vs controllers |
| EF Core and data | 25 | N+1 queries, tracking vs no-tracking, migrations in CI, transactions, indexing, SQL joins and window functions |
| Testing and practices | 20 | unit vs integration tests, `WebApplicationFactory`, mocking, SOLID, clean architecture, code review |
| Algorithms (concepts) | 25 | Big-O, hash maps, two pointers, sliding window, BFS/DFS, binary search — explain-the-approach questions, not full problems |
| System design (mid-level) | 15 | URL shortener, rate limiter, notification service, caching strategies, queues, idempotency |
| Behavioral | 40 | conflict, failure, tight deadline, disagreeing with a lead, mentoring, ambiguous requirements, why this company |

**Sourcing:** write answers yourself from Microsoft Learn docs and your own experience, then use the LLM to draft extra key points you review. Writing the model answers is itself the most valuable prep.

## Non-functional requirements

**Security**

- API key for the LLM lives in user secrets locally and Azure Key Vault (or app settings) in production; never sent to the browser.
- Single-user auth via ASP.NET Core Identity or an external login; every endpoint requires it.
- Phase 3 code runs only in Judge0's container sandbox with CPU, memory and network limits — never in the app process.

**Cost**

- Use a smaller, cheaper model for the Interviewer turns and a stronger model for evaluation.
- Per-session token cap and a monthly spend alert at the LLM provider.
- Expected usage: a few sessions a week, so hosting on a free or low tier is enough.

**Testing**

- Unit tests for SM-2 scheduling and `QuestionSelector` logic.
- Integration tests with `WebApplicationFactory` and Testcontainers Postgres.
- LLM calls behind an `ILlmClient` interface, faked in tests; a small eval set of saved answers checks the Evaluator after prompt changes.

**Deployment**

- GitHub Actions: build, test, deploy on merge to `main`.
- Azure App Service + Azure Database for PostgreSQL, or a single container on Fly.io/Render.
- Structured logging with Serilog; log prompt, token counts and latency per LLM call (not answer content in production logs).

## Build plan

About six weeks of evenings (8–10 hours a week) gets phases 1 and 2 live; start using the drill in week 2 rather than waiting for the AI parts.

Don't start phase 2 until you're drilling daily, and don't trust session scores until the Evaluator matches your own grading within a point.

**Risks**

| Risk | Mitigation |
| --- | --- |
| Building the app replaces actually preparing | Use the drill from week 2; cap build time per week |
| Evaluator grades too generously | Separate Evaluator role, temperature 0, calibration set |
| Thin question content makes the AI vague | Seed key points per question before phase 2 |
| LLM costs creep up | Cheaper model for turns, token cap, spend alert |
| Scope creep into code runner and voice | Phase 3 only after a month of real use |

**Open questions**

- [ ] Which LLM provider and models for Interviewer vs Evaluator?
- [ ] Blazor interactive server vs WebAssembly for the UI?
- [ ] Keep it personal, or make it multi-user as a portfolio showcase?
- [ ] Host on Azure (closer to target employers' stacks) or a cheaper container host?
