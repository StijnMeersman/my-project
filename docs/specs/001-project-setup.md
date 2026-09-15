# Feature Specification: Project Setup — Clients, Projects, People and Assignments

<!--
  TEMPLATE INSTRUCTIONS
  =====================
  Fill in each section focusing on WHAT the feature does and WHY — not HOW it should
  be implemented.

  Usage:
  - One spec per feature or functional slice
  - Store in docs/specs/ and version-control alongside your code
  - Use [NEEDS CLARIFICATION: question] markers for unresolved decisions (max 3)
  - Remove optional sections that don't apply — don't leave them as N/A
  - Reference your arc42 architecture docs where relevant rather than duplicating them
-->

## 1. Overview

| Field           | Value                                                                |
| --------------- | -------------------------------------------------------------------- |
| Feature ID      | 001                                                                  |
| Status          | Implemented (2026-09-15)                                             |
| Author          | stijn                                                                |
| Created         | 2026-09-15                                                           |
| Last updated    | 2026-09-15                                                           |
| Epic / Parent   | [Story map: Set up projects & people](../product/story-map.md) — stories 001, 002, 003 |
| Arc42 reference | 3. Context & Scope · 5. Building Block View · 8. Crosscutting Concepts · 10. Quality Requirements (all currently unpopulated — see §9.3) |

This spec covers three stories from the user story map as a single functional slice, because
they share one domain model and one actor:

| Map # | Story                                                                                      |
| ----- | ------------------------------------------------------------------------------------------ |
| 001   | As a manager, I create a project with a client and a predefined budget of hours              |
| 002   | As a manager, I assign team members to a project so only they can log hours against it       |
| 003   | As a manager, I see all my projects in one list with their budget and hours burned so far    |

### 1.1 Problem Statement

Time registration only works if the scaffolding exists first: hours must land on a *specific
project*, that project must carry a *predefined hour budget*, and only *assigned* people may
log against it. Today that scaffolding is informal — budgets live in a contract or a quote and
go unwatched until invoicing, project staffing is a verbal agreement, and project information
is scattered across spreadsheets and email with no single trustworthy view. The result is
overruns discovered too late and hours landing on the wrong project.

### 1.2 Goal

A manager can establish the system of record for their work: register the clients they serve,
create projects under those clients with an explicit hour budget, and assign the people allowed
to work on each one. One list shows every project with its budget, hours burned and hours
remaining, so budget is a number that is watched continuously rather than reconciled afterwards.

### 1.3 Non-Goals

- **Logging hours.** The weekly grid and time entries are stories 005–009. This slice only
  *consumes* burned hours as a read-only derived total.
- **Closing or archiving projects.** Explicitly story 004. No project status or archive flag is
  introduced here — adding one now would mean the project list needs a filter that nothing can
  yet set.
- **Authentication and authorization mechanics.** A Person is a record, not a credential. FR-018
  states *which* role may do what; how the current user is established and signed in belongs to a
  later story (see §10, Q1).
- **Money.** Budget is denominated in hours only. No rates, costs, invoicing or currency.
- **Approval flow.** Submission, review, approval and rejection are stories 010–016.
- **Reporting and export.** The project dashboard and payroll export are stories 018–020. The
  list in FR-013 is an operational view, not a reporting tool.
- **Notifications.** No email, reminders or nudges in this slice.
- **Editing or deleting clients and people.** This slice creates them; changing a person's role,
  correcting a client name, and deactivating either are deferred (see §10, Q2). Projects *are*
  editable (FR-008) because a wrong budget is immediately visible and immediately harmful.

## 2. User Stories

### US-001: Register a client

**As a** manager,
**I want** to register a client with a name,
**so that** projects can be grouped under the organisation they are done for.

### US-002: Add a person

**As a** manager,
**I want** to add a person to the organisation with their name, email and role,
**so that** there is someone to assign to projects.

### US-003: Create a project with a budget

**As a** manager,
**I want** to create a project for a client with a predefined budget of hours,
**so that** the work has a funded container to log time against.

### US-004: Assign people to a project

**As a** manager,
**I want** to assign people to a project,
**so that** only they can log hours against it.

### US-005: See all projects with budget and burn

**As a** manager,
**I want** to see all projects in one list with their budget and hours burned,
**so that** I notice an overrun while I can still act on it.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                                              | Priority | User Story  |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | -------- | ----------- |
| FR-001 | The system shall let a manager register a client with a name.                                                                                                             | Must     | US-001      |
| FR-002 | The system shall reject a client whose name duplicates an existing client, case-insensitively.                                                                            | Must     | US-001      |
| FR-003 | The system shall let a manager add a person with a full name, an email address and a role of Employee or Manager.                                                         | Must     | US-002      |
| FR-004 | The system shall reject a person whose email duplicates an existing person, case-insensitively.                                                                           | Must     | US-002      |
| FR-005 | The system shall let a manager create a project with a name, exactly one client, and an hour budget greater than zero.                                                    | Must     | US-003      |
| FR-006 | The system shall reject a project that has no client.                                                                                                                     | Must     | US-003      |
| FR-007 | The system shall reject a project name that duplicates an existing project **for the same client**, case-insensitively. Two different clients may each have a "Website".   | Must     | US-003      |
| FR-008 | The system shall let a manager edit a project's name, client and budget after creation.                                                                                   | Should   | US-003      |
| FR-009 | The system shall let a manager assign one or more people to a project.                                                                                                    | Must     | US-004      |
| FR-010 | The system shall prevent the same person from being assigned to the same project twice.                                                                                   | Must     | US-004      |
| FR-011 | The system shall let a manager remove a person's assignment from a project, warning first if that person has logged hours against it.                                     | Should   | US-004      |
| FR-012 | The system shall show, for each project, the people currently assigned to it.                                                                                             | Must     | US-004      |
| FR-013 | The system shall display a list of all projects showing name, client, budget, hours burned and hours remaining.                                                           | Must     | US-005      |
| FR-014 | The system shall calculate hours burned as the sum of **all** hours logged against the project, regardless of approval status.                                            | Must     | US-005      |
| FR-015 | The system shall calculate hours remaining as budget minus hours burned, and shall permit a negative value to represent an overrun.                                       | Must     | US-005      |
| FR-016 | The system shall visually distinguish projects that are over budget, and projects at or above 90% of budget, in the list.                                                 | Should   | US-005      |
| FR-017 | The system shall report zero hours burned for a project with no logged hours, rather than an empty or error value.                                                        | Must     | US-005      |
| FR-018 | The system shall restrict client creation, person creation, project creation, project editing and assignment management to users with the Manager role.                   | Must     | US-001–005  |

**Note on FR-014.** Counting draft and submitted hours — not just approved ones — is a
deliberate decision: the budget number is an early-warning signal that must move the moment
someone logs time, so an overrun is visible before the week is even submitted. The consequence
is that burned hours can decrease when a draft entry is edited or deleted. SC-018 pins this
behaviour so it is not silently "optimised" into approved-only later.

## 4. Acceptance Scenarios

### SC-001: Register a client (FR-001)

```gherkin
Given I am signed in as a manager
When I register a client named "Acme Corp"
Then "Acme Corp" appears in the list of clients
  And it is selectable when creating a project
```

### SC-002: Duplicate client name is rejected (FR-002)

```gherkin
Given a client named "Acme Corp" already exists
When I register a client named "ACME CORP"
Then the client is not created
  And I am told a client with that name already exists
```

### SC-003: Add a person (FR-003)

```gherkin
Given I am signed in as a manager
When I add a person named "Sam Rivers" with email "sam@acme.test" and role Employee
Then "Sam Rivers" appears in the list of people
  And they are selectable when assigning people to a project
```

### SC-004: Duplicate email is rejected (FR-004)

```gherkin
Given a person with email "sam@acme.test" already exists
When I add a person with email "SAM@ACME.TEST"
Then the person is not created
  And I am told that email is already in use
```

### SC-005: Create a project with a budget (FR-005)

```gherkin
Given a client "Acme Corp" exists
When I create a project "Website Redesign" for "Acme Corp" with a budget of 120 hours
Then the project exists with a budget of 120 hours
  And it shows 0 hours burned and 120 hours remaining
```

### SC-006: A project cannot exist without a client (FR-006)

```gherkin
Given I am signed in as a manager
When I create a project "Website Redesign" without selecting a client
Then the project is not created
  And I am told a client is required
```

### SC-007: Budget must be greater than zero (FR-005)

```gherkin
Given a client "Acme Corp" exists
When I create a project for "Acme Corp" with a budget of 0 hours
Then the project is not created
  And I am told the budget must be greater than zero
```

### SC-008: The same project name under two clients is allowed (FR-007)

```gherkin
Given a project "Website" exists for client "Acme Corp"
  And a client "Globex" exists
When I create a project "Website" for "Globex"
Then both projects exist independently
```

### SC-009: Duplicate project name for one client is rejected (FR-007)

```gherkin
Given a project "Website" exists for client "Acme Corp"
When I create a project "website" for "Acme Corp"
Then the project is not created
  And I am told that client already has a project with that name
```

### SC-010: Edit a project's budget (FR-008)

```gherkin
Given a project "Website Redesign" has a budget of 120 hours
When I change its budget to 160 hours
Then the project shows a budget of 160 hours
  And hours remaining is recalculated against the new budget
```

### SC-011: Cutting the budget below hours already burned (FR-008, FR-015)

```gherkin
Given a project has a budget of 120 hours and 60 hours burned
When I change its budget to 40 hours
Then the change is accepted
  And the project shows -20 hours remaining
  And the project is flagged as over budget
```

### SC-012: Assign people to a project (FR-009, FR-012)

```gherkin
Given a project "Website Redesign" exists
  And people "Sam Rivers" and "Kit Lowe" exist
When I assign both of them to "Website Redesign"
Then the project lists "Sam Rivers" and "Kit Lowe" as assigned
  And both may log hours against it
```

### SC-013: A person cannot be assigned twice (FR-010)

```gherkin
Given "Sam Rivers" is assigned to "Website Redesign"
When I assign "Sam Rivers" to "Website Redesign" again
Then the project still lists "Sam Rivers" exactly once
  And I am told they are already assigned
```

### SC-014: Unassigning a person who has logged hours (FR-011)

```gherkin
Given "Sam Rivers" is assigned to "Website Redesign"
  And "Sam Rivers" has logged 12 hours against it
When I remove their assignment
Then I am warned that 12 logged hours will remain on the project
When I confirm
Then "Sam Rivers" is no longer assigned
  And the project still counts those 12 hours as burned
  And "Sam Rivers" can no longer log new hours against it
```

### SC-015: The project list shows budget, burned and remaining (FR-013)

```gherkin
Given a project "Website Redesign" for "Acme Corp" has a budget of 120 hours
  And 45 hours have been logged against it
When I open the project list
Then I see "Website Redesign", client "Acme Corp", budget 120, burned 45, remaining 75
```

### SC-016: A project with no logged hours reads zero, not blank (FR-017)

```gherkin
Given a project was created and no hours have ever been logged against it
When I open the project list
Then the project shows 0 hours burned
  And hours remaining equals its full budget
```

### SC-017: Projects near and over budget are distinguished (FR-016)

```gherkin
Given project "Alpha" has a budget of 100 hours and 80 hours burned
  And project "Beta" has a budget of 100 hours and 95 hours burned
  And project "Gamma" has a budget of 100 hours and 110 hours burned
When I open the project list
Then "Alpha" is shown as within budget
  And "Beta" is highlighted as approaching its budget
  And "Gamma" is highlighted as over budget
```

### SC-018: Burned hours include unapproved time (FR-014)

```gherkin
Given a project has a budget of 100 hours
  And 10 approved hours, 15 submitted hours and 5 draft hours are logged against it
When I open the project list
Then the project shows 30 hours burned
  And 70 hours remaining
```

### SC-019: An employee cannot perform project setup (FR-018)

```gherkin
Given I am signed in as an employee
When I attempt to create a project, add a person or change an assignment
Then the action is refused
  And no data is changed
```

## 5. Domain Model

### 5.1 Entities

#### Client

An organisation that work is done for. Projects are always done for exactly one client.

| Attribute | Type     | Constraints                                       | Description                          |
| --------- | -------- | ------------------------------------------------- | ------------------------------------ |
| id        | UUID     | PK, generated                                     |                                      |
| name      | string   | required, trimmed, 1–200 chars, unique (case-insensitive) | Display name, e.g. "Acme Corp" |
| createdAt | datetime | generated, immutable                              |                                      |

#### Person

Someone who can be assigned to projects and log hours. In this slice a Person is a record only —
it carries no credentials and cannot sign in until the authentication story exists (§10, Q1).

| Attribute | Type     | Constraints                                                | Description                             |
| --------- | -------- | ---------------------------------------------------------- | --------------------------------------- |
| id        | UUID     | PK, generated                                              |                                         |
| fullName  | string   | required, trimmed, 1–200 chars                             | e.g. "Sam Rivers"                       |
| email     | EmailAddress | required, unique (case-insensitive)                    | Identifies the person                   |
| role      | enum     | required, one of [Employee, Manager]                       | Governs FR-018                          |
| createdAt | datetime | generated, immutable                                       |                                         |

#### Project

A funded container for work. Every logged hour belongs to exactly one project.

| Attribute   | Type     | Constraints                                               | Description                                  |
| ----------- | -------- | --------------------------------------------------------- | -------------------------------------------- |
| id          | UUID     | PK, generated                                             |                                              |
| name        | string   | required, trimmed, 1–200 chars, unique per client (case-insensitive) |                                    |
| clientId    | UUID     | required, FK → Client                                     | Exactly one, never null                      |
| budgetMinutes | Duration | required, > 0, ≤ 100 000 hours                          | The predefined budget set by the manager     |
| createdAt   | datetime | generated, immutable                                      |                                              |
| updatedAt   | datetime | generated, updated on every edit                          | Supports the last-write-wins rule in EC-8    |

> **Amended 2026-09-15.** `budgetHours: Hours` became `budgetMinutes: Duration` per
> [spec 005 §5.3](005-weekly-time-grid.md) and [ADR-0003](../architecture/adr/0003-durations-as-whole-minutes.md).
> A budget is still entered and displayed in hours; it is *held* in whole minutes.

#### Assignment

The manager's decision that a person may log hours against a project. A pure link with a stamp;
it has no state of its own.

| Attribute  | Type     | Constraints                                  | Description                            |
| ---------- | -------- | -------------------------------------------- | -------------------------------------- |
| id         | UUID     | PK, generated                                |                                        |
| projectId  | UUID     | required, FK → Project                       |                                        |
| personId   | UUID     | required, FK → Person                        |                                        |
| assignedAt | datetime | generated, immutable                         |                                        |
|            |          | unique together: (projectId, personId)       | Enforces FR-010                        |

#### TimeEntry — *referenced, not defined here*

Owned by story 006 and specified in its own spec. This slice depends on only three things from
it: a TimeEntry belongs to one **Project**, belongs to one **Person**, and carries a **duration**.
Burned hours (§5.4) is derived by summing those durations. Nothing in this spec constrains the rest
of the TimeEntry model.

### 5.2 Relationships

- A **Client** has many **Projects** (one-to-many).
- A **Project** belongs to exactly one **Client** — never zero.
- A **Project** relates to many **People** through **Assignment** (many-to-many).
- A **Person** may be assigned to many **Projects**, including none.
- A **Project** has many **TimeEntries** (one-to-many, defined in story 006).
- A **TimeEntry** references a **Person** independently of whether that person is *currently*
  assigned — the assignment can be removed while their logged hours remain (SC-014).

### 5.3 Value Objects

#### EmailAddress

| Attribute | Type   | Constraints                                                        |
| --------- | ------ | ------------------------------------------------------------------ |
| value     | string | required, ≤ 254 chars, must contain a local part, `@` and a domain |

Compared case-insensitively for uniqueness; stored as entered for display.

#### Duration

A non-negative quantity of time used for both budgets and logged amounts, held as a whole number of
minutes.

| Attribute | Type | Constraints                                     |
| --------- | ---- | ----------------------------------------------- |
| minutes   | int  | ≥ 0, ≤ 6 000 000 (100 000 hours)                |

Entered as either `hh:mm` ("120:30") or a decimal number of hours ("120", "7.5"), always parsed in
the invariant culture. A value that does not land on a whole minute is **refused, not rounded**
(EC-3). Displayed as `hh:mm`.

Remaining hours is *not* a Duration, because it may be negative (FR-015); it is a signed minute
count rendered with the same `hh:mm` format.

> **Amended 2026-09-15.** This section replaces the original `Hours` value object
> (decimal, 2 places). Twenty minutes is not representable as 2-decimal hours, and an integer sum
> satisfies NFR-004 by construction. See [spec 005 §5.3](005-weekly-time-grid.md) and
> [ADR-0003](../architecture/adr/0003-durations-as-whole-minutes.md).

### 5.4 Domain Rules and Invariants

- **A project always has a client.** `clientId` is never null, at creation or after an edit
  (FR-006).
- **A budget is always positive.** `budgetMinutes > 0` holds at creation *and* after every edit
  (FR-005). A budget of zero or a negative budget is never valid.
- **A budget may fall below what is already burned.** Budget is a plan, not a constraint on
  recorded reality. Lowering it below burned hours is accepted and puts the project over budget
  (SC-011). There is deliberately **no** `budget >= burned` invariant.
- **Burned hours is derived, never stored.** `burnedHours = sum of hours of all TimeEntries for
  the project, regardless of status`. It is computed on read, so it can never drift from the
  entries it summarises (FR-014, FR-017). For a project with no entries it is `0`, not null.
- **Burned hours is never negative.** It is a sum of non-negative Durations.
- **Remaining hours may be negative.** `remainingHours = budgetHours - burnedHours`. A negative
  value is the meaningful representation of an overrun and must not be clamped to zero (FR-015).
  Because both sides are whole minutes, the subtraction is exact.
- **Budget utilisation drives the list's warnings.** `utilisation = burnedHours / budgetHours`.
  A project is *over budget* when `burnedHours > budgetHours`, and *approaching budget* when
  `utilisation >= 0.90` and it is not yet over (FR-016, SC-017).
- **Client names are globally unique; project names are unique per client.** Both compared
  case-insensitively on the trimmed value. Two clients may each have a project called "Website"
  (FR-002, FR-007, SC-008).
- **A person's email is globally unique**, compared case-insensitively (FR-004).
- **An assignment is unique per (project, person).** Assigning someone already assigned is a
  no-op that reports the fact rather than creating a duplicate (FR-010).
- **Only an assigned person may log hours against a project.** This slice establishes the
  assignment; stories 005–009 enforce the rule when logging. Stated here because it is the entire
  reason Assignment exists.
- **Removing an assignment never deletes time entries.** Those hours were really worked; they stay
  on the project and keep counting toward burned (SC-014). History is not rewritten.
- **Role is not a hierarchy.** A Manager may also be assigned to projects and log hours. Role
  governs access to setup actions (FR-018), nothing else.

## 6. Non-Functional Requirements

Project-wide quality requirements belong in arc42 §10, which is currently an empty template.
The requirements below are specific to this feature; when §10 is populated, any that turn out to
be project-wide should move there and be referenced from here instead.

| ID      | Category      | Requirement                                                                                                                                          |
| ------- | ------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance   | The project list (FR-013) shall render in < 500 ms at p95 with 200 projects and 50 000 time entries, computing burned hours with a single aggregate query per page — not one query per project. |
| NFR-002 | Security      | The Manager-role restriction (FR-018) shall be enforced server-side on every create, edit and assignment action. Hiding the UI is not sufficient.       |
| NFR-003 | Consistency   | Burned and remaining hours shall be computed from live data on each read. No cached or precomputed totals, so the figures can never disagree with the underlying entries. |
| NFR-004 | Accuracy      | Hour amounts shall be stored and summed as whole minutes (§5.3). Floating-point accumulation is not acceptable: summing 1 000 entries must match the arithmetic sum exactly. *(Amended 2026-09-15 — was "exact decimals with 2 decimal places"; see [ADR-0003](../architecture/adr/0003-durations-as-whole-minutes.md).)* |
| NFR-005 | Scale         | The feature shall be designed for a single organisation of up to 100 people, 200 active projects and ~50 000 time entries per year. It is not multi-tenant. |
| NFR-006 | Usability     | Creating a project shall require a single screen and no more than three inputs (name, client, budget), so setup is not a barrier to using the app.       |
| NFR-007 | Traceability  | Project creation and every subsequent edit shall be timestamped (`createdAt`, `updatedAt`), so a surprising budget can at least be dated.                |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                             | Expected Behavior                                                                                                       |
| ----- | ------------------------------------------------------------------------------------ | ----------------------------------------------------------------------------------------------------------------------- |
| EC-1  | Manager submits an empty create form                                                 | Show per-field validation errors, persist nothing, retain whatever was typed                                              |
| EC-2  | Name is whitespace only, or has leading/trailing spaces                              | Trim before validating and storing; a whitespace-only name is rejected as empty                                           |
| EC-3  | Budget is non-numeric, negative, or has more than 2 decimal places                    | Reject with a message naming the rule; do not silently round                                                              |
| EC-4  | Budget above the 100 000-hour cap                                                    | Reject; this is a fat-finger guard (e.g. 4000 typed as 400000), not a business limit                                      |
| EC-5  | Name longer than 200 characters                                                      | Reject with the limit stated; do not truncate silently                                                                    |
| EC-6  | Email is malformed (no `@`, no domain)                                               | Reject before creating the person                                                                                         |
| EC-7  | Case-insensitive comparison on non-ASCII names (e.g. Turkish dotted/dotless i)        | Use culture-invariant case-insensitive comparison so uniqueness behaves identically regardless of server locale           |
| EC-8  | Two managers edit the same project's budget concurrently                             | Last write wins. `updatedAt` records which edit landed. Acceptable at the scale in NFR-005; revisit if it causes disputes  |
| EC-9  | Two managers assign the same person to the same project simultaneously               | The (projectId, personId) uniqueness constraint rejects the second write; surface it as "already assigned", not an error   |
| EC-10 | Project list is opened with no projects at all                                        | Show an empty state that points to project creation — not a blank table                                                   |
| EC-11 | A client exists with no projects                                                     | Valid. The client is still selectable; it simply contributes no rows to the project list                                  |
| EC-12 | Unassigning a person who has logged **zero** hours                                   | Remove immediately without the SC-014 warning — there is nothing at stake                                                 |
| EC-13 | A project is far over budget (e.g. 110 hours burned on a 10-hour budget)              | Display the true negative remaining (-100). Do not clamp, hide, or cap the overrun                                         |
| EC-14 | Time entries exist for a person no longer assigned to the project                    | Those hours still count toward burned (SC-014). The person appears in historical totals but not in the assigned list       |
| EC-15 | A project is reassigned to a different client via edit (FR-008), and the new client already has a project with that name | Reject the edit for the same reason FR-007 rejects the create; the uniqueness rule is evaluated against the *target* client |
| EC-16 | Deleting a client or a person                                                         | Out of scope in this slice (§1.3). No delete action is offered, so no orphaned project or assignment can be created        |

## 8. Success Criteria

| ID     | Criterion                                                                                                                                      |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-01 | All 19 acceptance scenarios (SC-001 … SC-019) pass as automated tests in CI                                                                     |
| SUC-02 | A manager can go from an empty database to a project with a budget and two assigned people in under two minutes, without consulting documentation |
| SUC-03 | For a seeded dataset of 1 000 time entries, the burned and remaining figures in the project list match the exact arithmetic sum, with no rounding drift (NFR-004) |
| SUC-04 | An automated test confirms that a user with the Employee role cannot create or modify any client, person, project or assignment (FR-018, NFR-002) |
| SUC-05 | The project list meets NFR-001 against a dataset of 200 projects and 50 000 time entries                                                        |
| SUC-06 | Every project in the system has exactly one client and a budget greater than zero — verifiable as a data invariant at any point in time          |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **Authentication and current-user resolution** — FR-018 and NFR-002 require knowing who the
  current user is and what role they hold. No story in the map covers this yet. Tracked as §10, Q1.
  *Resolved for this slice:* the current user is stubbed behind `ICurrentUser`
  ([ADR-0004](../architecture/adr/0004-stubbed-identity-until-authentication-lands.md)), which makes
  FR-018 fully testable but is not access control. Real authentication is still required.
- **Story 006 (TimeEntry)** — FR-014 sums time entries. Until the logging slice exists, every
  project correctly reports 0 hours burned (FR-017, SC-016), so this slice is independently
  shippable and demoable. SC-015, SC-017 and SC-018 cannot run until TimeEntry exists and should
  be written against seeded entry data. *Resolved as implemented:* `TimeEntry` and `TimesheetWeek`
  exist as read dependencies only, shaped to specs 005 and 010 and documented as owned by them;
  the acceptance tests seed entries directly.
- **Story 004 (close/archive)** — will add a project lifecycle state and an accompanying filter to
  the list built here. This spec deliberately leaves room for it rather than pre-building it.
- **Persistence approach** — *resolved:* EF Core 10 on SQLite with checked-in migrations, recorded
  as [ADR-0001](../architecture/adr/0001-ef-core-with-sqlite-for-persistence.md). The spec itself
  remains storage-agnostic.

### 9.2 Constraints

- ASP.NET Core Razor Pages on `net10.0`, nullable and implicit usings enabled.
- New C# files must use `namespace my_project.*` — the project's `RootNamespace` is `my_project`
  (underscore) while the directory and assembly are `my-project`.
- ~~No test project exists yet.~~ *Done:* `my-project.Tests` (xUnit) and a solution file were added
  for SUC-01 (`my-project.slnx`). The tests run against a real SQLite `:memory:` database, because every uniqueness rule
  in this spec is carried by a unique index that the EF InMemory provider would not enforce.
- The arc42 documentation set is a complete but entirely empty skeleton. This is the first feature
  to need crosscutting patterns (validation, error display, authorization), so whatever it
  establishes becomes the de facto project convention — it should be written back into §8.

### 9.3 Architecture References

All arc42 sections below are currently unpopulated placeholder templates. They are listed as the
sections this feature *should* inform and be constrained by; where a section is empty, this spec
establishes the first precedent rather than following one.

| Arc42 Section                    | Relevance to This Feature                                                                                       |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | Manager and Employee are the two communication partners; this slice defines no external system interfaces           |
| 5. Building Block View           | Introduces the first domain building blocks: Client, Person, Project, Assignment                                   |
| 6. Runtime View                  | Create-project and assign-people are the first write flows worth documenting                                       |
| 8. Crosscutting Concepts         | This feature sets the first patterns for input validation, uniqueness enforcement, error display and authorization  |
| 9. Architecture Decisions (ADRs) | **Written:** [ADR-0001](../architecture/adr/0001-ef-core-with-sqlite-for-persistence.md) persistence, [ADR-0002](../architecture/adr/0002-burned-hours-derived-never-stored.md) derived-not-stored burned hours (§5.4), [ADR-0003](../architecture/adr/0003-durations-as-whole-minutes.md) whole-minute durations, [ADR-0004](../architecture/adr/0004-stubbed-identity-until-authentication-lands.md) stubbed identity |
| 10. Quality Requirements         | NFR-001 and NFR-004 are strong candidates to be promoted to project-wide quality scenarios — still to do           |
| 11. Risks and Technical Debt     | **Written:** the stubbed identity, SQLite's single writer, and the re-aggregation cost are logged there            |
| 12. Glossary                     | **Written:** Client, Project, Person, Assignment, budget / burned / remaining hours, Duration and the rest          |

## 10. Open Questions

| #   | Question                                                                                                   | Owner | Status | Resolution                                                                                                   |
| --- | ------------------------------------------------------------------------------------------------------------ | ----- | ------ | -------------------------------------------------------------------------------------------------------------- |
| 1   | How is the current user established and signed in? FR-018 depends on it, and no story in the map covers auth. | stijn | **Deferred** | Stubbed for now: `ICurrentUser` with a dev-only cookie role switcher, so FR-018 is enforced and tested server-side without guessing at a scheme ([ADR-0004](../architecture/adr/0004-stubbed-identity-until-authentication-lands.md)). **This is not access control** — a story for real authentication is still needed before deployment. |
| 2   | Can clients and people be edited or deactivated after creation (correct a name, change a role, handle a leaver)? | stijn | Open   | Deferred out of this slice (§1.3). Likely a follow-up story; project editing (FR-008) is covered here.           |

---

<!--
  CHECKLIST
  =========
  - [x] Problem statement is clear and concise
  - [x] All user stories have acceptance scenarios
  - [x] Each functional requirement traces to a user story
  - [x] Domain model covers all entities mentioned in the requirements
  - [x] Domain rules and invariants are listed
  - [x] Edge cases cover failure modes, not just happy paths
  - [x] Non-functional requirements are specific and measurable
  - [x] Arc42 references point to the right sections (all currently empty templates — see 9.3)
  - [x] No more than 3 [NEEDS CLARIFICATION] markers remain (zero present)
  - [x] Open questions are assigned and have a resolution path
-->
