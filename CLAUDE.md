# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repository is

A **greenfield ASP.NET Core Razor Pages app** (`net10.0`, nullable + implicit usings enabled) created from the `dotnet new webapp` template — `Program.cs`, `Pages/Index|Privacy|Error`, Bootstrap/jQuery under `wwwroot/lib`. None of it has been customized yet.

The real content of the repo so far is a **planning pipeline** in `.claude/` and `docs/`: the app is meant to be designed through story mapping and specs *before* code is written. Expect early sessions to be planning work, not implementation.

## Commands

```bash
dotnet run                 # http://localhost:5281 (https profile also serves https://localhost:7078)
dotnet watch run           # hot reload during development
dotnet build
dotnet run --launch-profile https
```

There is no test project yet. When adding one, create a sibling project (e.g. `my-project.Tests`) and a `.sln`, since `my-project.csproj` is currently the only project. Single test: `dotnet test --filter "FullyQualifiedName~MyTest"`.

Note the project's `RootNamespace` is `my_project` (underscore) while the directory and assembly are `my-project` — new C# files must use `namespace my_project.*`.

## Planning workflow

The two custom tools are meant to be run in order, and the numbering links them:

1. **`/story-mapping`** (skill → `story-mapping` agent, `.claude/agents/story-mapping.md`) — interviews the user with `AskUserQuestion`, one question at a time, and writes `docs/product/story-map.md` with globally numbered stories (001, 002, …). This agent must not write code, and must not ask technical/stack questions. It has project-scoped memory at `.claude/agent-memory/story-mapping/MEMORY.md` and is expected to update it with goals, personas, and scope rationale.
2. **`/spec <story>`** (`.claude/skills/spec/SKILL.md`) — six-phase interactive spec writer producing `docs/specs/NNN-<slug>.md` from `.claude/skills/spec/spec-template.md`. **NNN is the story number from the story map**, not a new sequence.

Neither `docs/product/` nor `docs/specs/` exists yet; both are created on first use.

## Architecture documentation

`docs/architecture/` holds the full **arc42** skeleton (sections 01–12, indexed by `00-table-of-contents.md`). Every section is currently an empty template with placeholder tables and HTML comments. Specs are expected to *reference* arc42 sections rather than duplicate them — project-wide quality requirements belong in `10-quality-requirements.md`, feature-specific NFRs in the spec. ADRs go under `docs/architecture/adr/`, logged in the table in `09-architecture-decisions.md`.
