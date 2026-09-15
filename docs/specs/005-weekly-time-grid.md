# Feature Specification: The Weekly Time Grid — Rows, Entries and Live Budget

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
| Feature ID      | 005                                                                  |
| Status          | Draft                                                                |
| Author          | stijn                                                                |
| Created         | 2026-09-15                                                           |
| Last updated    | 2026-09-15                                                           |
| Epic / Parent   | [Story map: Log hours](../product/story-map.md) — stories 005, 006, 007 |
| Arc42 reference | 3. Context & Scope · 5. Building Block View · 6. Runtime View · 8. Crosscutting Concepts · 10. Quality Requirements (all currently unpopulated — see §9.3) |

This spec covers three stories from the user story map as a single functional slice, because
they are one screen and one act: you cannot enter hours without a grid to enter them into, and
the budget figure only means anything at the moment the hours are typed.

| Map # | Story                                                                                      |
| ----- | ------------------------------------------------------------------------------------------ |
| 005   | As an employee, I see my week as a grid with my assigned projects as rows and days as columns |
| 006   | As an employee, I enter hours on a specific day and project, with an optional note           |
| 007   | As an employee, I see a project's remaining budget while I am logging against it             |

This is the other half of the contract opened by [spec 001](001-project-setup.md): that spec
defines Client, Person, Project, Assignment and the derived burned-hours rule, and *references*
TimeEntry without defining it. **This spec defines TimeEntry.**

### 1.1 Problem Statement

An employee's hours only become trustworthy if logging them is a smaller act than remembering
them. Today there is no place to put a day's work at the moment it happens, so hours are
reconstructed on Friday from calendars and memory — and nobody sees a project eating its budget
until a manager finds it in a report. The employee needs their week as a single editable
surface, and needs the budget consequence of what they type visible while they type it.

### 1.2 Goal

An employee opens their week, pulls in the projects they actually worked on, types `hh:mm` into
weekday cells with an optional note, and saves a day at a time. Every project row carries that
project's live remaining budget — the same figure the manager sees in the project list — so an
overrun is noticed by the person creating it, on the day they create it, rather than by an
approver weeks later.

### 1.3 Non-Goals

- **Running totals per day and per week.** Story 008. A day's total *is* computed at save to
  enforce the 24-hour limit (FR-014), but displaying totals as a feature belongs to 008.
- **Copying last week's entries.** Story 009. This slice makes the grid; 009 makes it fast to fill.
- **Submitting a week, and week status (draft / submitted / approved / rejected).** Stories
  010–013. Entries here have no lifecycle. §5.4 defines the seam story 010 plugs into.
- **Warnings about an empty workday or a short week.** Story 011. The only warnings here are the
  daily cap (FR-015) and the budget (FR-020, FR-021).
- **Deadline reminders.** Story 012.
- **A manager seeing or approving someone else's week.** Stories 014–016. FR-022 restricts this
  screen to the signed-in employee's own week, and says nothing about what managers may see.
- **Weekend work.** A direct consequence of the Monday–Friday grid: weekend hours cannot be
  recorded at all. This is a domain rule (FR-004, §5.4), not a display choice.
- **Authentication and current-user resolution.** Inherited unresolved from spec 001 §10 Q1 —
  same stub, same dependency (§9.1).
- **Editing or deleting projects, clients, people or assignments.** Spec 001 owns those.

## 2. User Stories

### US-001: See my week

**As an** employee,
**I want** my week shown as weekday columns,
**so that** I can see and fill the whole week on one surface.

### US-002: Pull in only the projects I worked on

**As an** employee,
**I want** to add rows for the projects I actually touched this week,
**so that** my grid stays the size of my week, not the size of my assignment list.

### US-003: Enter hours with a note

**As an** employee,
**I want** to type hours on a day and project and optionally say what they were,
**so that** the record explains itself later.

### US-004: Save a day when it is right

**As an** employee,
**I want** to commit a day deliberately,
**so that** nothing is registered half-typed.

### US-005: See what my hours cost the budget

**As an** employee,
**I want** each project's remaining budget on its row,
**so that** I see the consequence of my hours while I am logging them.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                | Priority | User Story |
| ------ | -------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall display an employee's week as a grid of project rows by five weekday columns, Monday through Friday.                          | Must     | US-001     |
| FR-002 | The system shall open on the week containing today, and let the employee move to the previous or next week.                                    | Must     | US-001     |
| FR-003 | The system shall identify each row by project name and client.                                                                                 | Should   | US-001     |
| FR-004 | The system shall reject any attempt to record hours on a Saturday or Sunday.                                                                   | Must     | US-001     |
| FR-005 | The system shall let the employee add a row for any project they are currently assigned to, and shall offer no others.                         | Must     | US-002     |
| FR-006 | The system shall prevent a project appearing as more than one row in the same week.                                                            | Must     | US-002     |
| FR-007 | The system shall persist the row selection per person per week, so a row added with no hours yet survives a reload.                            | Must     | US-002     |
| FR-008 | The system shall let the employee remove a row that has no saved hours that week, and shall refuse to remove one that has.                     | Must     | US-002     |
| FR-009 | The system shall still show a row, read-only, for a project with saved hours that week when the employee is no longer assigned to it.          | Should   | US-002     |
| FR-010 | The system shall accept a cell value typed as `hh:mm` at whole-minute granularity.                                                             | Must     | US-003     |
| FR-011 | The system shall reject a malformed, negative, or greater-than-`24:00` cell value, naming the rule that was broken.                            | Must     | US-003     |
| FR-012 | The system shall let the employee attach an optional note of up to 500 characters to a cell that has hours, and shall indicate which cells carry one. | Must | US-003     |
| FR-013 | The system shall register a day's values only when that day is saved; unsaved edits are not recorded.                                          | Must     | US-004     |
| FR-014 | The system shall validate an entire day column in one pass at save, and shall reject the save whole — changing nothing — if the day's total exceeds `24:00`. | Must | US-004 |
| FR-015 | The system shall warn the employee when a saved day totals more than `12:00`, without preventing the save.                                      | Should   | US-004     |
| FR-016 | The system shall delete the time entry for a cell that is cleared and then saved.                                                              | Must     | US-004     |
| FR-017 | The system shall warn before unsaved day edits are discarded by navigating away or changing week.                                              | Should   | US-004     |
| FR-018 | The system shall show, on every project row, that project's remaining budget computed across all people and all logged hours.                  | Must     | US-005     |
| FR-019 | The system shall refresh every affected row's remaining budget after a day is saved.                                                           | Must     | US-005     |
| FR-020 | The system shall save hours that take a project over budget, showing the resulting negative remaining and flagging the row as over budget.     | Must     | US-005     |
| FR-021 | The system shall flag a row as approaching budget at 90% or more utilisation, matching spec 001 FR-016.                                        | Should   | US-005     |
| FR-022 | The system shall restrict an employee to viewing and editing only their own week.                                                              | Must     | US-001     |
| FR-023 | The system shall allow editing in the current week and any past week, and shall show weeks starting after the current week read-only.          | Should   | US-001     |

**Note on FR-013 and FR-014.** The unit of commit is a **day column** — every project row for
one weekday — not a cell and not the week. This is what makes FR-014 possible: the 24-hour rule
is a property of a *day*, so it can only be checked when the whole day is presented at once. The
cost is that at most one day's typing is at risk in a closed tab, which FR-017 mitigates.

**Note on FR-018.** The figure shown is the *whole project's* remaining budget across every
person, identical to the one a manager sees in spec 001 FR-013. There is no per-person budget in
the domain, so any narrower figure would be a different and less meaningful number.

## 4. Acceptance Scenarios

### SC-001: The week opens on today (FR-001, FR-002)

```gherkin
Given I am signed in as an employee
When I open my timesheet
Then I see the week containing today
  And I see columns for Monday, Tuesday, Wednesday, Thursday and Friday
  And I see no column for Saturday or Sunday
```

### SC-002: Move to another week (FR-002)

```gherkin
Given I am viewing the week of 14 September 2026
When I move to the previous week
Then I see the week of 7 September 2026
  And I see the rows and hours saved for that week
```

### SC-003: A future week cannot be filled in (FR-023)

```gherkin
Given today falls in the week of 14 September 2026
When I move forward to the week of 21 September 2026
Then I can see the week
  And I cannot enter hours or add rows
  And I am told future weeks cannot be filled in yet
```

### SC-004: Add a project row (FR-005, FR-007, FR-018)

```gherkin
Given I am assigned to "Website Redesign" and "Mobile App"
  And my week has no rows
When I add the project "Website Redesign"
Then "Website Redesign" appears as a row with five empty weekday cells
  And the row shows its client and its remaining budget
```

### SC-005: Only assigned projects are offered (FR-005)

```gherkin
Given I am assigned to "Website Redesign" only
  And a project "Internal Tooling" exists that I am not assigned to
When I open the list of projects I can add
Then I see "Website Redesign"
  And I do not see "Internal Tooling"
```

### SC-006: A project cannot be added twice in one week (FR-006)

```gherkin
Given "Website Redesign" is already a row in my week
When I add "Website Redesign" again
Then my week still shows "Website Redesign" exactly once
  And I am told it is already on this week
```

### SC-007: An empty row survives a reload (FR-007)

```gherkin
Given I added "Website Redesign" and entered no hours
When I reload my timesheet
Then "Website Redesign" is still a row in my week
```

### SC-008: Remove an empty row (FR-008)

```gherkin
Given "Mobile App" is a row in my week with no saved hours
When I remove the row
Then "Mobile App" is no longer in my week
  And it can be added again
```

### SC-009: A row with hours cannot be removed (FR-008)

```gherkin
Given "Website Redesign" is a row in my week with 4:00 saved on Tuesday
When I try to remove the row
Then the row remains
  And I am told to clear its hours first
```

### SC-010: Hours remain visible after I am unassigned (FR-009)

```gherkin
Given I saved 6:00 against "Website Redesign" on Monday
  And my manager has since removed my assignment to "Website Redesign"
When I open that week
Then "Website Redesign" still shows 6:00 on Monday
  And I cannot change or add hours on that row
```

### SC-011: Enter hours and save the day (FR-010, FR-013)

```gherkin
Given "Website Redesign" is a row in my week
When I type "7:30" in its Monday cell and save Monday
Then Monday shows 7:30 against "Website Redesign"
  And 450 minutes are registered against that project, for me, on that date
```

### SC-012: Unsaved hours are not registered (FR-013)

```gherkin
Given I typed "7:30" in the Monday cell and did not save Monday
When I reload my timesheet
Then the Monday cell is empty
  And no hours are registered for Monday
```

### SC-013: A malformed value is rejected (FR-011)

```gherkin
Given "Website Redesign" is a row in my week
When I type "seven" in its Monday cell and save Monday
Then nothing is saved for Monday
  And I am told hours must be entered as hh:mm
```

### SC-014: A single cell over 24 hours is rejected (FR-011)

```gherkin
Given "Website Redesign" is a row in my week
When I type "25:00" in its Monday cell and save Monday
Then nothing is saved for Monday
  And I am told a cell cannot exceed 24:00
```

### SC-015: A day totalling over 24 hours rejects the whole save (FR-014)

```gherkin
Given my week has rows for "Website Redesign", "Mobile App" and "Internal Tooling"
When I type "10:00", "9:00" and "6:00" in their Monday cells and save Monday
Then no part of Monday is saved
  And I am told Monday totals 25:00 and cannot exceed 24:00
```

### SC-016: A long day saves with a warning (FR-015)

```gherkin
Given my week has two project rows
When I type "9:00" and "4:30" in their Monday cells and save Monday
Then both entries are saved
  And I am warned that Monday totals 13:30
```

### SC-017: Add a note to an entry (FR-012)

```gherkin
Given I typed "3:00" in the Tuesday cell of "Website Redesign"
When I add the note "Design review with client" and save Tuesday
Then the entry is saved with that note
  And the cell is marked as carrying a note
```

### SC-018: Clearing a cell deletes the entry (FR-016)

```gherkin
Given 4:00 is saved on Wednesday against "Mobile App"
When I clear the Wednesday cell and save Wednesday
Then no hours are registered for "Mobile App" on Wednesday
  And the project's burned hours drop by 4:00
```

### SC-019: Leaving with unsaved edits warns first (FR-017)

```gherkin
Given I typed "5:00" in a Thursday cell and have not saved Thursday
When I move to another week
Then I am warned that Thursday has unsaved hours
  And I can go back and save, or continue and lose them
```

### SC-020: Every row shows remaining budget (FR-018)

```gherkin
Given "Website Redesign" has a budget of 120:00
  And 45:00 have been logged against it by everyone so far
When I open my week with "Website Redesign" as a row
Then the row shows 75:00 remaining
```

### SC-021: Remaining budget updates when I save (FR-019)

```gherkin
Given the "Website Redesign" row shows 75:00 remaining
When I save 8:00 on Monday against it
Then the row shows 67:00 remaining
```

### SC-022: Going over budget saves and warns (FR-020)

```gherkin
Given the "Website Redesign" row shows 3:00 remaining
When I save 8:00 on Monday against it
Then the 8:00 is saved
  And the row shows -5:00 remaining
  And the row is flagged as over budget
```

### SC-023: Approaching budget is flagged before the cliff (FR-021)

```gherkin
Given "Mobile App" has a budget of 100:00 and 88:00 logged against it
When I save 4:00 against it
Then the row shows 8:00 remaining
  And the row is flagged as approaching its budget
```

### SC-024: I can only reach my own week (FR-022)

```gherkin
Given a colleague "Kit Lowe" has a timesheet
When I attempt to open Kit Lowe's week
Then I am refused
  And I see only my own timesheet
```

### SC-025: Only a weekday can hold hours (FR-004)

```gherkin
Given any project row in my week
When hours are submitted for a Saturday or Sunday date
Then they are rejected
  And nothing is registered
```

## 5. Domain Model

### 5.1 Entities

#### TimeEntry

Hours worked by one person, on one project, on one day. This is the entity spec 001 §5.1
references but deliberately does not define.

| Attribute | Type     | Constraints                                    | Description                         |
| --------- | -------- | ---------------------------------------------- | ----------------------------------- |
| id        | UUID     | PK, generated                                  |                                     |
| personId  | UUID     | required, FK → Person                          | Who worked the hours                |
| projectId | UUID     | required, FK → Project                         | What they were worked on            |
| date      | date     | required, must be Monday–Friday                | The day worked, a calendar date     |
| duration  | Duration | required, > 0, ≤ 1440 minutes                  | Stored as whole minutes             |
| note      | string   | optional, trimmed, ≤ 500 chars                 | Free text explaining the hours      |
| createdAt | datetime | generated, immutable                           |                                     |
| updatedAt | datetime | generated, updated on every edit               |                                     |
|           |          | unique together: (personId, projectId, date)   | One cell holds exactly one entry    |

#### WeekRow

The employee's decision that a project belongs on their week. It exists independently of whether
any hours were entered — which is precisely why it must be stored: a row added and not yet filled
has no time entries to be derived from.

| Attribute     | Type     | Constraints                                            | Description                     |
| ------------- | -------- | ------------------------------------------------------ | ------------------------------- |
| id            | UUID     | PK, generated                                          |                                 |
| personId      | UUID     | required, FK → Person                                  | Whose week                      |
| weekStartDate | date     | required, must be a Monday                             | Identifies the week             |
| projectId     | UUID     | required, FK → Project                                 | The row's project               |
| addedAt       | datetime | generated, immutable                                   |                                 |
|               |          | unique together: (personId, weekStartDate, projectId)  | Enforces FR-006                 |

#### Project, Person, Assignment — *referenced, not defined here*

Owned by [spec 001](001-project-setup.md). This slice consumes: **Project** for its name, client
and budget; **Person** as the owner of entries; **Assignment** as the gate on which projects may
be added and edited.

### 5.2 Relationships

- A **Person** has many **TimeEntries**; each TimeEntry belongs to exactly one Person.
- A **Project** has many **TimeEntries** — this is the relationship spec 001 §5.4 sums to derive
  burned hours.
- A **WeekRow** links one **Person**, one week and one **Project**.
- Every **TimeEntry** has a corresponding **WeekRow** for its person, project and week. Entries
  never exist off-grid.
- A **WeekRow** may exist with no **TimeEntries** — an added but unfilled row.
- **Assignment** governs which projects may be *added* and *written to*. It does not own existing
  entries: removing an assignment leaves the WeekRow and its entries intact (SC-010, and spec 001
  SC-014).

### 5.3 Value Objects

#### Duration

A length of time, held as a whole number of minutes. Entered and displayed as `hh:mm`.

| Attribute | Type    | Constraints                                  |
| --------- | ------- | -------------------------------------------- |
| minutes   | integer | ≥ 0; ≤ 1440 for a single time entry          |

All arithmetic is integer arithmetic, so summation is exact by construction and no rounding rule
is ever needed.

> **Amendment to spec 001.** Spec 001 §5.3 defines `Hours` as a decimal with at most 2 decimal
> places, which cannot represent `1:20` (1.333… hours). `Hours` is therefore **replaced by this
> minute-backed `Duration`**, and `Project.budgetHours` becomes `budgetMinutes` with an `hh:mm`
> projection for display. Spec 001's NFR-004 and SUC-03 then hold trivially. Remaining budget
> stays a *signed* minute count, because it may be negative (spec 001 FR-015). See §10, Q2.

### 5.4 Domain Rules and Invariants

- **Hours land on weekdays only.** A `TimeEntry.date` is Monday–Friday. Weekend work is
  unrecordable by design (FR-004). Revisiting this means adding columns *and* relaxing this rule.
- **No empty entries.** `duration > 0`. Clearing a cell deletes the entry rather than storing a
  zero (FR-016), so a note can never exist without hours for it to explain.
- **One entry per cell.** `(personId, projectId, date)` is unique. Re-saving a day updates in
  place rather than appending.
- **A day holds at most 24 hours.** For one person and one date, the sum of all entry durations is
  ≤ 1440 minutes. This is a property of the day, enforced across the whole column at save, not of
  any single cell (FR-014).
- **A day saves whole or not at all.** No partial commit. A rejected day leaves every cell in it
  exactly as it was (SC-015).
- **A week is Monday-anchored.** `weekStartDate` is always a Monday; an entry belongs to the week
  whose Monday is the most recent one on or before its date.
- **Only assigned projects can be added or written to.** Checked at the moment of writing against
  the *current* assignment, not the assignment as it stood when the row was added (FR-005).
- **Unassignment freezes, it never deletes.** Existing rows and entries stay visible and keep
  counting toward burned hours; no new hours may be added to them (SC-010).
- **Editing is limited to the current and past weeks.** A week starting after the current week is
  read-only (FR-023). Past weeks stay open because correcting last week is the normal case.
- **Remaining budget is derived, never stored.** `remaining = project.budgetMinutes − sum of the
  durations of all time entries for that project, across all people`. This is the same rule as
  spec 001 §5.4 — one definition, two audiences. It is computed on read, so the employee's figure
  and the manager's figure can never disagree.
- **Remaining may be negative, and the save still happens.** Over budget is a flag on a row, never
  a block (FR-020). The hours were really worked.
- **Budget flags match spec 001 exactly.** Over budget when `burned > budget`; approaching budget
  when `utilisation ≥ 0.90` and not yet over (FR-021, spec 001 FR-016).
- **Entries carry no lifecycle in this slice.** No draft / submitted / approved state exists here.
  Story 010 will introduce a week-level status keyed on `(personId, weekStartDate)` — the same key
  `WeekRow` already uses — and will make entries read-only once a week leaves draft. That field is
  deliberately left unbuilt rather than guessed at now.

## 6. Non-Functional Requirements

Project-wide quality requirements belong in arc42 §10, which is currently an empty template. The
requirements below are specific to this feature.

| ID      | Category     | Requirement                                                                                                                                                      |
| ------- | ------------ | ------------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| NFR-001 | Performance  | The week grid shall render in < 500 ms at p95 with 20 rows, computing every row's remaining budget with a single aggregate query across the visible projects — not one query per row. |
| NFR-002 | Performance  | Saving a day shall complete in < 300 ms at p95 for a day column of up to 20 rows.                                                                                  |
| NFR-003 | Accuracy     | Durations shall be stored, summed and compared as integer minutes. No floating-point arithmetic at any point in the path from cell to budget figure.               |
| NFR-004 | Reliability  | A day save shall be atomic: every cell in the column commits, or none does (FR-014). A failure mid-save shall leave the day as it was.                             |
| NFR-005 | Security     | The own-week restriction (FR-022) and the assignment check (FR-005) shall be enforced server-side on every read and write. Hiding UI is not sufficient.            |
| NFR-006 | Consistency  | Remaining budget shall be read live on each render and each save, never cached, per spec 001 NFR-003.                                                              |
| NFR-007 | Usability    | Filling a five-day week shall require no more than five saves, and the grid shall be fully operable from the keyboard — entry speed is the point of the feature.   |
| NFR-008 | Durability   | At most one day column of unsaved typing shall ever be at risk, and the employee shall be warned before losing it (FR-017).                                        |
| NFR-009 | Scale        | The feature shall be designed for up to 20 rows in a week per person, within spec 001 NFR-005's envelope of 100 people and ~50 000 entries per year.               |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                   | Expected Behavior                                                                                                       |
| ----- | ------------------------------------------------------------------------------------------ | ------------------------------------------------------------------------------------------------------------------------- |
| EC-1  | Employee is assigned to no projects at all                                                  | Show an empty state saying no projects are assigned yet, pointing at the manager. Do not show a grid with no rows and no explanation |
| EC-2  | Employee has assignments but has added no rows this week                                    | Show the grid frame with a prompt to add a project — a distinct state from "rows added, no hours yet"                      |
| EC-3  | Cell entered as `7` with no colon                                                           | Accept as 7:00. A bare number is hours                                                                                     |
| EC-4  | Cell entered as `1:75` or `1:60`                                                            | Reject; minutes must be 0–59. Do not normalise to 2:15                                                                     |
| EC-5  | Cell entered as `0:00`                                                                      | Treated as clearing the cell (FR-016). No zero-duration entry is ever stored                                               |
| EC-6  | Note typed, then the cell is cleared before saving                                          | The note is discarded with the entry. A note cannot outlive its hours                                                      |
| EC-7  | Note longer than 500 characters                                                             | Reject with the limit stated; do not truncate silently                                                                     |
| EC-8  | One valid and one invalid cell in the same day column                                       | Reject the whole day (FR-014), keep both cells as typed, and mark the invalid one                                          |
| EC-9  | The same employee has the grid open in two tabs and saves the same day in both              | Last save wins for that day column. The second save overwrites the day as a whole, including deleting cells it shows empty  |
| EC-10 | A manager changes the project's budget while the employee is typing                         | Nothing stale is persisted — remaining is derived on read (§5.4). The row shows the new figure after the next save or reload |
| EC-11 | A manager removes the employee's assignment while they have unsaved hours for that project  | The day save is rejected, naming the project that is no longer assigned. The employee clears that row and saves the rest     |
| EC-12 | Project is already over budget before the employee adds a row                               | The row is still addable and shows the negative remaining immediately, flagged as over budget                              |
| EC-13 | Project has a budget and no entries at all                                                  | Remaining equals the full budget; the row is unflagged                                                                     |
| EC-14 | A row is added and never filled, and weeks pass                                             | The empty WeekRow persists harmlessly. It is not auto-removed, and it contributes nothing to any total                     |
| EC-15 | Employee navigates to a week before they had any assignment                                 | The week renders empty and editable; they may add any project they are assigned to *now*                                    |
| EC-16 | The grid is open as the clock rolls from Sunday into Monday                                 | The week on screen does not silently change. The new week becomes "current" on the next load                                |
| EC-17 | Week spans a month or year boundary                                                         | Irrelevant to the model — the grid is the five weekdays from its Monday, regardless of calendar boundaries                   |
| EC-18 | Daylight-saving transition falls inside the week                                            | No effect. `date` is a calendar date and `duration` is a count of minutes; neither is an instant, so neither is converted     |
| EC-19 | Hours are submitted for a date outside the week currently displayed                         | Reject. A day save only ever writes to dates within its own week                                                            |
| EC-20 | Employee attempts to write to a project they were never assigned to                         | Reject server-side (NFR-005), even though the UI never offered that project                                                 |

## 8. Success Criteria

| ID     | Criterion                                                                                                                              |
| ------ | ---------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-01 | All 25 acceptance scenarios (SC-001 … SC-025) pass as automated tests in CI                                                              |
| SUC-02 | An employee can fill a five-day week across three projects, keyboard only, in under 60 seconds                                            |
| SUC-03 | Summing 1 000 seeded entries yields exactly the arithmetic total, with no rounding drift anywhere between cell and budget figure (NFR-003) |
| SUC-04 | An automated test confirms an employee cannot read or write another person's entries (FR-022, NFR-005)                                   |
| SUC-05 | An automated test confirms a day save that violates the 24-hour rule leaves every cell in that day unchanged (FR-014, NFR-004)            |
| SUC-06 | The grid meets NFR-001 with 20 rows against spec 001's 50 000-entry dataset                                                              |
| SUC-07 | No time entry exists with a weekend date or a non-positive duration — verifiable as a data invariant at any point in time                 |
| SUC-08 | The remaining-budget figure an employee sees on a row equals the figure the manager sees for that project in spec 001 FR-013, at all times |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 001](001-project-setup.md) — hard dependency.** Project, Person, Assignment and the
  budget must exist before a single cell can be typed. This slice cannot ship or be demoed first.
- **The `Hours` → `Duration` amendment to spec 001** (§5.3, §10 Q2) must land, or `hh:mm` entry
  silently violates spec 001's own accuracy requirement.
- **Authentication and current-user resolution** — FR-022 and NFR-005 need to know who is signed
  in. Still uncovered by any story in the map; inherited from spec 001 §10 Q1.
- **Story 010 (submit the week)** will add the week-level status this slice deliberately omits, and
  will make entries read-only once a week leaves draft. The `(personId, weekStartDate)` key is
  already in place for it.
- **Story 008 (running totals)** builds on the day-total figure this slice computes for FR-014.
- **Story 009 (copy last week)** writes into the WeekRow and TimeEntry model defined here; it must
  respect the day-save commit boundary rather than bypassing it.
- **Story 004 (close/archive a project)** will need to decide whether a closed project can still be
  added as a row. Out of scope here; no lifecycle state exists on Project yet.

### 9.2 Constraints

- ASP.NET Core Razor Pages on `net10.0`, nullable and implicit usings enabled.
- New C# files must use `namespace my_project.*` — the project's `RootNamespace` is `my_project`
  (underscore) while the directory and assembly are `my-project`.
- No test project exists yet. SUC-01 requires creating a sibling project (e.g. `my-project.Tests`)
  and a `.sln`.
- The grid is an interactive, partially-updating screen — the first in this app. Whatever approach
  is chosen for the per-day save (full post, partial update, client-side state) becomes the
  project's precedent and should be recorded as an ADR.

### 9.3 Architecture References

All arc42 sections below are currently unpopulated placeholder templates.

| Arc42 Section                    | Relevance to This Feature                                                                                       |
| -------------------------------- | ----------------------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | The employee is the primary actor here, as the manager was in spec 001                                             |
| 5. Building Block View           | Adds TimeEntry and WeekRow to the building blocks introduced by spec 001                                            |
| 6. Runtime View                  | The day-save flow — validate column, commit atomically, recompute budgets — is the first non-trivial runtime flow worth diagramming |
| 8. Crosscutting Concepts         | Sets the project's patterns for interactive editing, unsaved-state warnings, atomic multi-record writes and `hh:mm` parsing |
| 9. Architecture Decisions (ADRs) | Needs an ADR for minute-backed durations (§5.3) and one for the interactive grid / partial-update approach          |
| 10. Quality Requirements         | NFR-003 and NFR-004 are strong candidates for promotion to project-wide quality scenarios                          |
| 12. Glossary                     | Time entry, week row, duration, day total, remaining budget should be entered here                                 |

## 10. Open Questions

| #   | Question                                                                                                     | Owner | Status | Resolution                                                                                                      |
| --- | -------------------------------------------------------------------------------------------------------------- | ----- | ------ | ------------------------------------------------------------------------------------------------------------------ |
| 1   | How is the current user established and signed in? FR-022 and NFR-005 depend on it.                            | stijn | **Deferred** | Inherited from spec 001 §10 Q1, which is now stubbed behind `ICurrentUser` ([ADR-0004](../architecture/adr/0004-stubbed-identity-until-authentication-lands.md)). A real authentication story is still needed. |
| 2   | Spec 001 still defines `Hours` as a 2-decimal number, which `hh:mm` entry breaks.                              | stijn | **Resolved 2026-09-15** | Done. Spec 001 §5.1 and §5.3 now define `budgetMinutes: Duration`, and `Duration` is implemented as a whole-minute value object in `Domain/Duration.cs`. Rationale in [ADR-0003](../architecture/adr/0003-durations-as-whole-minutes.md). |

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
