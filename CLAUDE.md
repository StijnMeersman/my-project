# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

A **time-registration app**: ASP.NET Core Razor Pages on `net10.0` (nullable + implicit usings), backed by EF Core on SQLite.

Work is driven by a **planning pipeline** in `.claude/` and `docs/`: features are story-mapped and specced *before* they are built, and a spec is the authority for what the code must do. Spec 001 (clients, people, projects, assignments, the project list) is implemented; specs 005 and 010 are written but not built.

### Layout

| Folder | Holds |
| --- | --- |
| `Domain/` | Entities and value objects. Validation lives here, returned as `Result`/`Result<T>` rather than thrown. |
| `Data/` | `TimeRegistrationDbContext`, `Migrations/`, and `DemoData` (development seed only). |
| `Application/` | Services (`ClientService`, `PersonService`, `ProjectService`, `AssignmentService`), `ICurrentUser`, view records. |
| `Web/` | `[ManagerOnly]` page filter, `ModelState` helpers. |
| `Pages/` | Razor Pages. |
| `my-project.Tests/` | xUnit, one file per spec section: `AcceptanceScenarios`, `EdgeCases`, `SuccessCriteria`. |

### Conventions worth knowing before editing

- **`RootNamespace` is `my_project`** (underscore) while the directory and assembly are `my-project` — new C# files use `namespace my_project.*`.
- The web project sits at the **repository root**, so the Web SDK globs everything beneath it. `my-project.csproj` explicitly `Remove`s `my-project.Tests\**`; keep that if you add another sibling project.
- **Durations are whole minutes**, never decimal hours — `Domain/Duration.cs`, [ADR-0003](docs/architecture/adr/0003-durations-as-whole-minutes.md).
- **Burned hours are never stored**, only aggregated in the read query — [ADR-0002](docs/architecture/adr/0002-burned-hours-derived-never-stored.md).
- **Uniqueness is a unique index over a persisted `Normalized*` column** (`ToUpperInvariant()`), plus a friendly pre-check query. Not SQLite `NOCASE`, which folds ASCII only.
- **The current user is a stub.** `DevRoleSwitchCurrentUser` reads a `dev-role` cookie set by the switcher in the layout. Anyone can become a Manager — see [ADR-0004](docs/architecture/adr/0004-stubbed-identity-until-authentication-lands.md). Do not deploy this.
- Manager-only rules are enforced **twice**: `[ManagerOnly]` on the page and `currentUser.RequireManager()` in the service. The service call is the one that matters.

## Commands

```bash
dotnet run                 # http://localhost:5281 (https profile also serves https://localhost:7078)
dotnet watch run           # hot reload during development
dotnet build my-project.slnx
dotnet test                # 53 tests: acceptance scenarios, edge cases, success criteria
dotnet test --filter "FullyQualifiedName~SC_001"
dotnet ef migrations add <Name> --output-dir Data/Migrations
```

Migrations run automatically at startup (`db.Database.MigrateAsync()`), and Development additionally seeds `DemoData` into an empty database. Delete `timeregistration.db` to start over.

Tests use a real SQLite `Data Source=:memory:` database, not the EF InMemory provider — the unique indexes carry most of spec 001's rules and InMemory enforces none of them.

## Planning workflow

The two custom tools are meant to be run in order, and the numbering links them:

1. **`/story-mapping`** (skill → `story-mapping` agent, `.claude/agents/story-mapping.md`) — interviews the user with `AskUserQuestion`, one question at a time, and writes `docs/product/story-map.md` with globally numbered stories (001, 002, …). This agent must not write code, and must not ask technical/stack questions. It has project-scoped memory at `.claude/agent-memory/story-mapping/MEMORY.md` and is expected to update it with goals, personas, and scope rationale.
2. **`/spec <story>`** (`.claude/skills/spec/SKILL.md`) — six-phase interactive spec writer producing `docs/specs/NNN-<slug>.md` from `.claude/skills/spec/spec-template.md`. **NNN is the story number from the story map**, not a new sequence.

`docs/product/story-map.md` and `docs/specs/` now exist. When implementing a spec, treat its acceptance scenarios, edge cases and success criteria as the test plan — `my-project.Tests` is organised to mirror them one-to-one, so a failure names the requirement it broke. If the implementation forces a change to the spec, edit the spec and say so; the two are not allowed to disagree.

## GitHub issues

Whenever issues come up — creating, reading, searching, commenting, updating, closing — **use the GitHub MCP server** (`mcp__github__*` tools), not the `gh` CLI or the web UI.

The target repository is always **https://github.com/StijnMeersman/my-project** (`owner: StijnMeersman`, `repo: my-project`) unless the user names a different one explicitly.

## Architecture documentation

`docs/architecture/` holds the full **arc42** skeleton (sections 01–12, indexed by `00-table-of-contents.md`). Sections 09, 11 and 12 are populated; 01–08 and 10 are still empty templates. Specs are expected to *reference* arc42 sections rather than duplicate them — project-wide quality requirements belong in `10-quality-requirements.md`, feature-specific NFRs in the spec. ADRs go under `docs/architecture/adr/`, logged in the table in `09-architecture-decisions.md`.
