# Feature Specification: Submitting the Week — Status, Warnings and Nudges

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
| Feature ID      | 010                                                                  |
| Status          | Draft                                                                |
| Author          | stijn                                                                |
| Created         | 2026-09-15                                                           |
| Last updated    | 2026-09-15                                                           |
| Epic / Parent   | [Story map: Submit the week](../product/story-map.md) — stories 010, 011, 012, 013 |
| Arc42 reference | 3. Context & Scope · 5. Building Block View · 6. Runtime View · 7. Deployment View · 8. Crosscutting Concepts · 9. ADRs · 10. Quality Requirements (all currently unpopulated — see §9.3) |

This spec covers four stories as a single functional slice, because they are four faces of one
thing: **a week has a state**. Submitting is the transition, the warnings are what guards it, the
nudges are what drives it, and the status is what the employee sees of it. Specifying them apart
would mean defining the same state machine three times.

| Map # | Story                                                                                      |
| ----- | ------------------------------------------------------------------------------------------ |
| 010   | As an employee, I submit my week for approval when I am done                                 |
| 011   | As an employee, I am warned before submitting if a workday is empty or my week is short on hours |
| 012   | As an employee, I get nudged before the submission deadline so I never forget my week        |
| 013   | As an employee, I see the status of each of my weeks: draft, submitted, approved or rejected |

This is the seam [spec 005](005-weekly-time-grid.md) §5.4 deliberately left unbuilt: *"Story 010
will introduce a week-level status keyed on `(personId, weekStartDate)` — the same key `WeekRow`
already uses — and will make entries read-only once a week leaves draft."* **This spec defines
TimesheetWeek**, and amends spec 005 accordingly (§5.4, "Amendment to spec 005").

It also defines the *states* `Approved` and `Rejected` and the transitions into them, because the
employee-facing statuses in story 013 are meaningless otherwise. The **manager's review screen**
that drives those transitions is story 016 and is out of scope here (§1.3).

### 1.1 Problem Statement

Hours that sit in a grid forever are not hours anyone can act on. Payroll and billing need a
moment where an employee says "this week is done and correct", and a manager needs a stable set of
numbers to review rather than a surface still being typed into. Today there is no such moment: the
grid has no lifecycle, nothing distinguishes a half-filled Tuesday from a finished week, nothing
tells an employee they forgot Thursday, and nothing reminds them at all until a manager chases
them on the phone.

### 1.2 Goal

Every week an employee owns carries exactly one of four states — **Draft, Submitted, Approved,
Rejected** — visible at a glance across all their weeks and on the grid itself. Submitting is one
deliberate action that freezes the week's entries and hands them to a manager. Before it lands,
the system says out loud what looks wrong — an empty workday, a short week — and then gets out of
the way, because the employee is the one who knows whether Thursday was really a day off. And
nobody reaches the deadline by surprise: the week nudges its owner on Friday afternoon, on the
morning of the deadline, and every weekday it stays overdue.

### 1.3 Non-Goals

- **The manager's review and approval screen.** Story 016 owns *how* a manager opens a submitted
  week, approves it, and writes a rejection comment. This spec defines the states, the legal
  transitions, the data a decision records, and what the employee sees of it — nothing about the
  manager's UI, queue or workflow.
- **The manager's "who is late" overview.** Story 014. This spec derives the lateness of a single
  week (§5.4); 014 aggregates it across people.
- **Flagging suspicious entries for the manager.** Story 017. The warnings here are employee-facing,
  evaluated at submission, and never block. 017 is a different audience with different thresholds.
- **Reopening or correcting an approved week.** Approved is terminal in this slice (§5.4). A
  correction after approval needs a manager-initiated reopen, which belongs with story 016 or a new
  story. See §10, Q3.
- **Exporting approved hours.** Story 020 consumes the `Approved` state defined here.
- **Per-person contract hours.** The expected weekly hours used by FR-011 is a single
  organisation-wide figure (§5.3). Part-time contracts would make it a property of Person and are
  deliberately not modelled yet — see §10, Q2.
- **Absence, leave and public holidays.** There is no absence model, so an empty Thursday is
  indistinguishable from a day off. This is precisely why the empty-workday warning is a warning
  and not a block (FR-014).
- **Changing hours, notes, rows or the grid itself.** Spec 005 owns all of that. This slice only
  adds a gate in front of it.
- **Authentication and current-user resolution.** Inherited unresolved from spec 001 §10 Q1 and
  spec 005 §10 Q1 — same stub, same dependency (§9.1).
- **A general-purpose notification system.** FR-023/FR-024 need exactly one message type delivered
  on a schedule. Nothing here justifies a notification framework, a preference centre or per-user
  channel settings.

## 2. User Stories

### US-001: Hand in my week

**As an** employee,
**I want** to submit my finished week for approval in one action,
**so that** my hours are on their way and I am not the one holding up payroll.

### US-002: Take it back if I was too quick

**As an** employee,
**I want** to withdraw a week I submitted by mistake, as long as nobody has decided on it,
**so that** a slip does not cost me a rejection.

### US-003: Be told what looks wrong before I hand it in

**As an** employee,
**I want** the system to point out empty workdays and a short week before I submit,
**so that** I catch what I forgot while it is still cheap to fix.

### US-004: Never be surprised by the deadline

**As an** employee,
**I want** to be nudged before — and after — the submission deadline,
**so that** forgetting my week is something the system prevents rather than something I get chased for.

### US-005: See where every week of mine stands

**As an** employee,
**I want** the status of each of my weeks in one list,
**so that** I know what is done, what is waiting on a manager, and what still needs me.

### US-006: Understand and fix a rejection

**As an** employee,
**I want** to see why a week was rejected and be taken straight into fixing it,
**so that** a rejection is a short round trip rather than a mystery.

## 3. Functional Requirements

| ID     | Requirement                                                                                                                                     | Priority | User Story |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------------------- | -------- | ---------- |
| FR-001 | The system shall let an employee submit a week of their own that is in Draft or Rejected status, moving it to Submitted.                            | Must     | US-001     |
| FR-002 | The system shall refuse to submit a week that has no saved hours at all, stating that an empty week cannot be submitted.                            | Must     | US-001     |
| FR-003 | The system shall refuse to submit a week that starts after the current week, matching the read-only rule in spec 005 FR-023.                        | Must     | US-001     |
| FR-004 | The system shall record the moment of submission on the week.                                                                                      | Must     | US-001     |
| FR-005 | The system shall make every time entry and week row of a Submitted or Approved week read-only, rejecting any attempt to add, change or delete them. | Must     | US-001     |
| FR-006 | The system shall restrict submitting and withdrawing a week to the employee that week belongs to.                                                   | Must     | US-001     |
| FR-007 | The system shall let an employee withdraw a Submitted week that has not yet been decided, returning it to Draft and making it editable again.       | Should   | US-002     |
| FR-008 | The system shall refuse to withdraw a week that is Approved or Rejected.                                                                           | Must     | US-002     |
| FR-009 | The system shall evaluate all submission warnings server-side at the moment of submission, against the hours as they are saved at that moment.      | Must     | US-003     |
| FR-010 | The system shall warn when any weekday of the week has no saved hours, naming each empty day.                                                       | Must     | US-003     |
| FR-011 | The system shall warn when the week's total is below the expected weekly hours, stating the total and the shortfall.                                | Must     | US-003     |
| FR-012 | The system shall warn when the week's total exceeds the long-week threshold, stating the total.                                                     | Should   | US-003     |
| FR-013 | The system shall warn when the week contains hours on a project that is over budget, naming each such project.                                      | Could    | US-003     |
| FR-014 | The system shall submit the week once the employee explicitly acknowledges the warnings shown, and shall never refuse a submission on a warning alone. | Must   | US-003     |
| FR-015 | The system shall record which warnings were acknowledged with the submission, so a reviewer can see what the employee was told.                     | Should   | US-003     |
| FR-016 | The system shall submit without an acknowledgement step when no warning applies.                                                                    | Should   | US-003     |
| FR-017 | The system shall nudge an employee on the Friday afternoon of a week that is not yet submitted.                                                     | Must     | US-004     |
| FR-018 | The system shall nudge an employee on the morning of the deadline day for a week that is not yet submitted.                                         | Must     | US-004     |
| FR-019 | The system shall nudge an employee on each following weekday, for up to five occurrences, while a week remains unsubmitted after its deadline.      | Should   | US-004     |
| FR-020 | The system shall state in every nudge which week it concerns, the hours logged so far, and when the deadline is or was.                             | Must     | US-004     |
| FR-021 | The system shall not deliver a nudge for a week that is Submitted or Approved at the moment of delivery.                                            | Must     | US-004     |
| FR-022 | The system shall deliver each nudge at most once per employee, per week, per occurrence, per channel.                                               | Must     | US-004     |
| FR-023 | The system shall show outstanding nudges inside the application, as a dismissible banner linking to the week concerned.                             | Must     | US-004     |
| FR-024 | The system shall additionally send each nudge to the employee's email address.                                                                     | Should   | US-004     |
| FR-025 | The system shall not nudge a person who has no project assignment covering that week.                                                              | Should   | US-004     |
| FR-026 | The system shall show the employee a list of their own weeks, each with its status: Draft, Submitted, Approved or Rejected.                         | Must     | US-005     |
| FR-027 | The system shall show, for each week in that list, the week's date range, its total hours and its deadline.                                         | Must     | US-005     |
| FR-028 | The system shall mark an unsubmitted week whose deadline has passed as overdue, without introducing a fifth status.                                 | Should   | US-005     |
| FR-029 | The system shall show a week's submission time, decision time and deciding manager once a decision exists.                                          | Should   | US-005     |
| FR-030 | The system shall default the week list to the most recent twelve weeks, newest first, and allow reaching older weeks.                               | Should   | US-005     |
| FR-031 | The system shall show the current week's status on the weekly grid itself, and shall say why the grid is read-only when it is.                      | Must     | US-005     |
| FR-032 | The system shall show the manager's rejection comment on a Rejected week, wherever that week's status is shown.                                     | Must     | US-006     |
| FR-033 | The system shall lead the employee from a Rejected week directly into editing that week's grid.                                                    | Should   | US-006     |
| FR-034 | The system shall move a week to Approved or Rejected only from Submitted, only on a manager's decision, and shall require a comment on a rejection. | Must     | US-005, US-006 |

**Note on FR-005.** Submission freezes the week — not just the numbers, but the row selection too.
A submitted week that could still gain a row would present a manager with a moving target. This is
the amendment to spec 005 FR-005…FR-016: every write path in the grid now asks the week's status
first. Spec 005 FR-023 (current and past weeks editable) still holds, and is *narrowed* by this,
never widened: a past week that is Approved is not editable.

**Note on FR-014.** Every warning here is advisory by construction. There is no absence model
(§1.3), so the system cannot know that an empty Thursday was a day off, and a system that blocks on
what it cannot know turns into a system people work around. The only hard refusals are FR-002
(nothing to submit), FR-003 (a week that has not happened) and FR-006 (not your week).

**Note on FR-019.** Five overdue nudges, then silence. An unsubmitted week eventually becomes the
manager's problem (story 014), not a matter for more email. The cap is what keeps a forgotten week
from generating mail forever.

**Note on FR-034.** Stated here because the state machine belongs to one spec, and story 013 cannot
describe four statuses if only two of them have a defined origin. Story 016 owns the manager's
screen; it must not invent transitions beyond the five in §5.4.

## 4. Acceptance Scenarios

### SC-001: Submit a complete week (FR-001, FR-004, FR-016)

```gherkin
Given my week of 14 September 2026 is in Draft
  And I have saved 8:00 on each weekday across my projects
When I submit the week
Then the week's status is Submitted
  And the submission time is recorded
  And I was not asked to acknowledge any warning
```

### SC-002: A submitted week is frozen (FR-005)

```gherkin
Given my week of 14 September 2026 is Submitted
When I open that week's grid
Then I cannot change a cell, add a row or remove a row
  And I am told the week was submitted and is awaiting approval
```

### SC-003: An empty week cannot be submitted (FR-002)

```gherkin
Given my week of 14 September 2026 has no saved hours
When I submit the week
Then the week stays in Draft
  And I am told an empty week cannot be submitted
```

### SC-004: A future week cannot be submitted (FR-003)

```gherkin
Given today falls in the week of 14 September 2026
When I attempt to submit the week of 21 September 2026
Then the week is not submitted
  And I am told a week can only be submitted once it has started
```

### SC-005: I can only submit my own week (FR-006)

```gherkin
Given my colleague "Kit Lowe" has a Draft week with hours in it
When I attempt to submit Kit Lowe's week
Then I am refused
  And Kit Lowe's week stays in Draft
```

### SC-006: Withdraw a submission (FR-007)

```gherkin
Given my week of 14 September 2026 is Submitted and undecided
When I withdraw it
Then the week's status is Draft
  And I can change its cells and rows again
  And its submission time is cleared
```

### SC-007: An approved week cannot be withdrawn (FR-008)

```gherkin
Given my week of 7 September 2026 is Approved
When I attempt to withdraw it
Then it stays Approved
  And I am told an approved week can no longer be changed
```

### SC-008: An empty workday is called out (FR-010, FR-014)

```gherkin
Given my week has 8:00 on Monday, Tuesday, Wednesday and Friday
  And Thursday has no saved hours
When I submit the week
Then I am warned that Thursday has no hours
  And the week is not yet submitted
When I acknowledge the warning and confirm
Then the week's status is Submitted
```

### SC-009: A short week is called out (FR-011)

```gherkin
Given the expected week is 40:00
  And my week totals 31:30
When I submit the week
Then I am warned that my week totals 31:30, which is 8:30 short of 40:00
  And I can acknowledge and submit anyway
```

### SC-010: Several warnings at once (FR-010, FR-011, FR-014)

```gherkin
Given my week has hours on Monday and Tuesday only, totalling 16:00
When I submit the week
Then I am warned that Wednesday, Thursday and Friday have no hours
  And I am warned that the week is 24:00 short of 40:00
When I acknowledge both and confirm
Then the week's status is Submitted
```

### SC-011: An unusually long week is called out (FR-012)

```gherkin
Given the long-week threshold is 60:00
  And my week totals 63:00
When I submit the week
Then I am warned that my week totals 63:00
  And I can acknowledge and submit anyway
```

### SC-012: Hours on an over-budget project are called out (FR-013)

```gherkin
Given "Website Redesign" is over budget
  And my week has 12:00 saved against "Website Redesign"
When I submit the week
Then I am warned that "Website Redesign" is over budget
  And I can acknowledge and submit anyway
```

### SC-013: Acknowledged warnings travel with the submission (FR-015)

```gherkin
Given my week has an empty Thursday and totals 32:00
When I acknowledge both warnings and submit
Then the week is Submitted
  And the week records that the empty-workday and short-week warnings were acknowledged
```

### SC-014: Warnings are recomputed on the server (FR-009)

```gherkin
Given my week has an empty Thursday
When a submission is sent that claims no warnings apply
Then the submission is not accepted as-is
  And the empty-workday warning is returned for acknowledgement
```

### SC-015: The Friday nudge (FR-017, FR-020, FR-021)

```gherkin
Given my week of 14 September 2026 is in Draft with 24:00 logged
When Friday 18 September 2026 15:00 arrives
Then I am nudged about the week of 14–18 September
  And the nudge states 24:00 logged and a deadline of Monday 21 September 12:00
```

### SC-016: The deadline-day nudge (FR-018)

```gherkin
Given my week of 14 September 2026 is still in Draft
When Monday 21 September 2026 09:00 arrives
Then I am nudged that the week of 14–18 September is due at 12:00 today
```

### SC-017: Overdue nudges stop after five (FR-019)

```gherkin
Given my week of 14 September 2026 is still unsubmitted after its deadline
When five weekday mornings have passed since the deadline day
Then I have received exactly five overdue nudges for that week
  And no further nudge is sent for it
```

### SC-018: Submitting silences the nudges (FR-021)

```gherkin
Given a nudge is scheduled for my week of 14 September 2026
When I submit that week before the nudge is delivered
Then the nudge is not delivered
  And no further nudge is sent for that week
```

### SC-019: A rejected week starts being nudged again (FR-019, FR-021)

```gherkin
Given my week of 14 September 2026 was rejected after its deadline had passed
When the next weekday morning arrives
Then I am nudged that the week still needs to be submitted
```

### SC-020: A nudge is never delivered twice (FR-022)

```gherkin
Given the deadline-day nudge for my week of 14 September 2026 has been delivered
When nudge delivery runs again for that occurrence
Then I do not receive a second copy
```

### SC-021: Nobody to nudge (FR-025)

```gherkin
Given "Kit Lowe" has no project assignment covering the week of 14 September 2026
When nudges are delivered for that week
Then Kit Lowe is not nudged
```

### SC-022: My weeks and their statuses (FR-026, FR-027, FR-030)

```gherkin
Given I have weeks in Draft, Submitted, Approved and Rejected
When I open my weeks
Then I see the twelve most recent weeks, newest first
  And each shows its date range, its total hours, its deadline and its status
```

### SC-023: An overdue draft is marked (FR-028)

```gherkin
Given my week of 7 September 2026 is in Draft
  And its deadline of 14 September 2026 12:00 has passed
When I open my weeks
Then that week is shown as Draft and marked overdue
  And its status is still Draft, not a separate status
```

### SC-024: A decided week shows who decided and when (FR-029)

```gherkin
Given my week of 7 September 2026 was approved by "Dana Ilves"
When I open my weeks
Then that week shows as Approved
  And it shows when I submitted it, when it was approved, and that Dana Ilves approved it
```

### SC-025: The grid shows the week's status (FR-031)

```gherkin
Given my week of 7 September 2026 is Approved
When I open that week's grid
Then I see that the week is approved
  And I see that it can no longer be changed
  And no cell accepts input
```

### SC-026: A rejection explains itself and leads back to the grid (FR-032, FR-033)

```gherkin
Given my week of 7 September 2026 was rejected with the comment "Tuesday is booked to the wrong project"
When I open my weeks
Then the week shows as Rejected with that comment
  And I can go straight from it into editing that week's grid
  And its cells accept input again
```

### SC-027: Resubmitting after a rejection (FR-001)

```gherkin
Given my week of 7 September 2026 is Rejected
  And I have corrected Tuesday's entry
When I submit the week again
Then its status is Submitted
  And its rejection comment is no longer presented as current
```

### SC-028: Only a submitted week can be decided (FR-034)

```gherkin
Given my week of 14 September 2026 is in Draft
When an approval is attempted on it
Then it is refused
  And the week stays in Draft
```

### SC-029: A rejection needs a reason (FR-034)

```gherkin
Given my week of 14 September 2026 is Submitted
When a rejection is attempted with no comment
Then the rejection is refused
  And the week stays Submitted
```

## 5. Domain Model

### 5.1 Entities

#### TimesheetWeek

One employee's week, and the state it is in. This is the entity spec 005 §5.4 anticipated but
deliberately did not define. It is keyed exactly as `WeekRow` is — `(personId, weekStartDate)` —
so every row and entry of a week resolves to one state without a new join.

| Attribute            | Type      | Constraints                                                      | Description                                        |
| -------------------- | --------- | ---------------------------------------------------------------- | -------------------------------------------------- |
| id                   | UUID      | PK, generated                                                    |                                                    |
| personId             | UUID      | required, FK → Person                                            | Whose week                                         |
| weekStartDate        | date      | required, must be a Monday                                       | Identifies the week                                |
| status               | enum      | required, one of [Draft, Submitted, Approved, Rejected]          | Defaults to Draft                                  |
| submittedAt          | datetime  | nullable; set on entering Submitted, cleared on withdrawal        | The moment of FR-004                               |
| acknowledgedWarnings | set<enum> | each one of [EmptyWorkday, ShortWeek, LongWeek, OverBudgetProject]; empty unless submitted | What the employee was told at submission (FR-015)  |
| decidedAt            | datetime  | nullable; required when Approved or Rejected                     |                                                    |
| decidedByPersonId    | UUID      | nullable, FK → Person; required when Approved or Rejected        | Must be a person with the Manager role             |
| decisionComment      | string    | optional, trimmed, ≤ 1000 chars; **required when Rejected**       | The manager's reason (FR-032)                      |
| createdAt            | datetime  | generated, immutable                                             |                                                    |
| updatedAt            | datetime  | generated, updated on every transition                           | Basis for the optimistic check in EC-9             |
|                      |           | unique together: (personId, weekStartDate)                        | One state per person per week                      |

A week with no `TimesheetWeek` record is **Draft** (§5.4). The record is created on the first
transition out of Draft, or whenever a week is first touched — it is never required for a week to
exist.

#### SubmissionNudge

One scheduled reminder occurrence for one person and one week. Stored rather than computed, because
FR-022 — never nudge the same person twice for the same occasion — is a claim about what has
already been delivered, and nothing derived can make it.

| Attribute     | Type     | Constraints                                                             | Description                                  |
| ------------- | -------- | ----------------------------------------------------------------------- | -------------------------------------------- |
| id            | UUID     | PK, generated                                                           |                                              |
| personId      | UUID     | required, FK → Person                                                   | Who is nudged                                |
| weekStartDate | date     | required, must be a Monday                                              | Which week                                   |
| kind          | enum     | required, one of [FridayAfternoon, DeadlineDay, Overdue]                | FR-017, FR-018, FR-019                       |
| scheduledFor  | datetime | required                                                                | Distinguishes the daily Overdue occurrences  |
| channel       | enum     | required, one of [InApp, Email]                                         | FR-023, FR-024                               |
| deliveredAt   | datetime | nullable                                                                | Null until delivered                         |
| suppressed    | boolean  | required, default false                                                 | True when skipped because the week was submitted (FR-021) |
| dismissedAt   | datetime | nullable; InApp only                                                    | When the employee dismissed the banner       |
|               |          | unique together: (personId, weekStartDate, kind, scheduledFor, channel)  | Enforces FR-022                              |

#### TimeEntry, WeekRow — *referenced, not defined here*

Owned by [spec 005](005-weekly-time-grid.md). This slice does not change their shape; it gates
every write to them on the status of the `TimesheetWeek` they belong to (FR-005).

#### Person, Project, Assignment — *referenced, not defined here*

Owned by [spec 001](001-project-setup.md). This slice consumes **Person** as the owner of a week
and as the decider of one, **Assignment** as the test in FR-025, and **Project** budget state for
the warning in FR-013.

### 5.2 Relationships

- A **Person** has many **TimesheetWeeks**; each TimesheetWeek belongs to exactly one Person.
- A **TimesheetWeek** covers the same `(personId, weekStartDate)` as zero or more **WeekRows** and,
  through them, zero or more **TimeEntries**. The state applies to all of them at once.
- A **TimesheetWeek** may be decided by one **Person** with the Manager role — never by its own
  owner (§5.4).
- A **Person** has many **SubmissionNudges**; each nudge concerns exactly one person and one week.
- A **SubmissionNudge** does *not* reference a TimesheetWeek row, only its key — nudges exist
  precisely for weeks that may have no record yet.

### 5.3 Value Objects

#### WeekPeriod

A working week, identified by its Monday.

| Attribute     | Type | Constraints                |
| ------------- | ---- | -------------------------- |
| weekStartDate | date | required, must be a Monday |

Its workdays are the five dates from `weekStartDate` through `weekStartDate + 4`. Its deadline and
nudge moments are derived from it by `TimesheetPolicy`. Displayed as a range, e.g. "14–18 September
2026".

#### TimesheetPolicy

The organisation's single set of timesheet expectations. One instance for the whole system — the
app is explicitly not multi-tenant (spec 001 NFR-005) — held as configuration, not as user-editable
data in this slice.

| Attribute                | Type     | Constraints                     | Default                       |
| ------------------------ | -------- | ------------------------------- | ----------------------------- |
| expectedWeeklyMinutes    | integer  | > 0                             | 2400 (40:00)                  |
| longWeekThresholdMinutes | integer  | > expectedWeeklyMinutes         | 3600 (60:00)                  |
| deadlineDayOffset        | integer  | days after weekStartDate        | 7 (the following Monday)      |
| deadlineTimeOfDay        | time     |                                 | 12:00                         |
| fridayNudgeTimeOfDay     | time     |                                 | 15:00                         |
| morningNudgeTimeOfDay    | time     |                                 | 09:00                         |
| maxOverdueNudges         | integer  | ≥ 0                             | 5                             |
| timeZone                 | string   | IANA zone id                    | `Europe/Brussels`             |

Every clock-dependent rule in this spec — the deadline, the three nudge moments, whether a week is
overdue — is evaluated in `timeZone`. Nothing in this feature is evaluated in UTC or in the
browser's zone, so two employees never disagree about whether a week is late.

#### Duration — *referenced, not defined here*

Minute-backed, from spec 005 §5.3. Week totals, shortfalls and thresholds are all whole minutes,
so SC-009's "8:30 short" is exact arithmetic, never a rounded figure.

### 5.4 Domain Rules and Invariants

- **A week has exactly one state.** `(personId, weekStartDate)` is unique on TimesheetWeek, and
  `weekStartDate` is always a Monday — the same key and the same anchoring as `WeekRow`.
- **Absence of a record means Draft.** A week nobody has ever touched is Draft, not "missing". Every
  read of a week's status resolves to one of the four values; no fifth value and no null exists.
- **The state machine has exactly five transitions.** `Draft → Submitted` (employee, FR-001),
  `Rejected → Submitted` (employee, FR-001), `Submitted → Draft` (employee withdrawal, FR-007),
  `Submitted → Approved` (manager, FR-034), `Submitted → Rejected` (manager, FR-034). Every other
  transition is refused. Story 016 must not add to this list.
- **Approved is terminal.** Nothing leaves Approved in this slice. Correcting an approved week needs
  a reopen that does not exist yet (§1.3, §10 Q3).
- **Only Draft and Rejected are editable.** A Submitted or Approved week rejects every write to its
  entries and rows (FR-005). Rejected is editable on purpose — a rejection exists to be fixed.
- **Editability is the intersection of two rules.** Spec 005 FR-023 already limits editing to the
  current and past weeks; status narrows that further. A week is editable when it is *both* not in
  the future *and* in Draft or Rejected.
- **An empty week is not submittable.** The sum of a week's entry durations must be > 0 to leave
  Draft (FR-002). Submitting nothing communicates nothing.
- **A future week is not submittable.** `weekStartDate` must be the current week's Monday or earlier
  (FR-003), consistent with the grid being read-only for future weeks.
- **Warnings never block; only the three hard rules do.** The hard refusals are empty week, future
  week and not-your-week. Every other objection is an acknowledgeable warning (FR-014), because the
  system has no absence model and therefore cannot know better than the employee.
- **Warnings are computed, never trusted.** The warning set is derived server-side from the saved
  hours at the moment of submission (FR-009). A client's claim about which warnings apply is an
  input to compare against, never a substitute.
- **The four warning conditions.** *EmptyWorkday*: some date in Monday–Friday of the week has no
  entries. *ShortWeek*: week total < `expectedWeeklyMinutes`. *LongWeek*: week total >
  `longWeekThresholdMinutes`. *OverBudgetProject*: the week has entries on a project whose burned
  hours exceed its budget, using the same over-budget test as spec 001 FR-016 and spec 005 FR-020.
- **Acknowledgement is recorded, not enforced afterwards.** `acknowledgedWarnings` is a snapshot of
  what was shown at submission. It is cleared on withdrawal and rewritten on each resubmission; it
  is never used to suppress a future warning.
- **A submission changes state and nothing else.** No entry, note, row or duration is created,
  altered or deleted by submitting, withdrawing, approving or rejecting. The hours mean the same
  thing in every state.
- **Burned hours ignore status.** Spec 001 FR-014 holds unchanged: Draft, Submitted, Approved and
  Rejected hours all count against a project's budget. Submission is not what makes hours real.
- **A rejection carries a reason.** `decisionComment` is required when `status = Rejected` and
  optional when Approved (FR-034). A rejection without a reason is not a rejection, it is a bounce.
- **A decision names its decider.** `decidedByPersonId` and `decidedAt` are set together with the
  status, and the decider must hold the Manager role.
- **Nobody decides their own week.** `decidedByPersonId ≠ personId`, even for a manager who logs
  hours — spec 001 §5.4 permits managers to log time, and this is where that permission stops.
- **A week's deadline is derived, never stored.** `deadline = weekStartDate + deadlineDayOffset at
  deadlineTimeOfDay`, in the policy time zone — by default the following Monday at 12:00. Changing
  the policy moves every deadline, past and future, because none were ever written down.
- **Overdue is derived, not a status.** A week is overdue when it is Draft or Rejected and the
  deadline has passed (FR-028). It stays Draft; overdue is an adjective, not a state.
- **A late submission is a valid submission.** Passing the deadline never closes submission. The
  deadline drives nudges and the manager's late list (story 014); it is not a lock.
- **Nudges are per occurrence and delivered at most once.** `(personId, weekStartDate, kind,
  scheduledFor, channel)` is unique (FR-022). A delivery run that repeats is a no-op.
- **Status at delivery decides, not status at scheduling.** A nudge scheduled while a week was Draft
  is suppressed if the week is Submitted or Approved when it comes due (FR-021). A week that is
  Rejected at delivery time *is* nudged — it needs the employee again.
- **The overdue nudge stops.** At most `maxOverdueNudges` overdue occurrences per week, on weekday
  mornings only (FR-019). After that, the week is the manager's problem.
- **No assignment, no nudge.** A person with no assignment covering the week has nothing to log and
  is not nudged (FR-025).

> **Amendment to spec 005.** Spec 005 FR-005 through FR-016 describe writes to a week's rows and
> cells without reference to any state, because none existed. Each of them is now conditional on
> the week being editable per the rule above. Specifically: the day save (spec 005 FR-013, FR-014)
> and row add/remove (FR-005, FR-008) must refuse on a Submitted or Approved week and say why, and
> spec 005 FR-023's read-only treatment of future weeks extends to submitted and approved weeks.
> Spec 005 §5.4's placeholder — "entries carry no lifecycle in this slice" — is superseded by this
> section.

## 6. Non-Functional Requirements

Project-wide quality requirements belong in arc42 §10, which is currently an empty template. The
requirements below are specific to this feature.

| ID      | Category     | Requirement                                                                                                                                                              |
| ------- | ------------ | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| NFR-001 | Performance  | Submitting a week — warning evaluation plus the state transition — shall complete in < 500 ms at p95 for a week of up to 20 rows and 100 entries.                            |
| NFR-002 | Performance  | The week list (FR-026) shall render 12 weeks in < 300 ms at p95, computing every week's total with a single aggregate query — not one query per week.                       |
| NFR-003 | Reliability  | The state transition, its timestamps and its acknowledged-warning snapshot shall commit atomically. A failed submission shall leave the week exactly as it was.              |
| NFR-004 | Reliability  | Nudge delivery shall be idempotent under at-least-once execution: a run repeated after a crash shall deliver nothing twice (FR-022).                                        |
| NFR-005 | Reliability  | Email delivery failure (FR-024) shall not prevent or delay the in-app nudge (FR-023), and shall not mark the occurrence delivered.                                          |
| NFR-006 | Timeliness   | A due nudge shall be delivered within 15 minutes of its scheduled moment.                                                                                                  |
| NFR-007 | Security     | Ownership (FR-006), the Manager role for decisions (FR-034) and the read-only rule (FR-005) shall be enforced server-side on every read and write. Hiding UI is not sufficient. |
| NFR-008 | Correctness  | Warning evaluation shall be a pure function of the week's saved entries and the policy, so the same week always yields the same warnings (FR-009).                          |
| NFR-009 | Consistency  | Deadlines, nudge moments and overdue status shall be evaluated in the policy time zone (§5.3), never in UTC or a client zone, including across daylight-saving transitions.  |
| NFR-010 | Scale        | Nudge evaluation shall complete for the whole organisation — spec 001 NFR-005's 100 people — in under one minute per run, with a single pass over unsubmitted weeks.         |
| NFR-011 | Traceability | Every transition shall record its moment and, for decisions, its actor, so any week's history can be reconstructed from the record alone.                                   |
| NFR-012 | Usability    | Submitting a clean week shall take one action and no confirmation step (FR-016); submitting a week with warnings shall take exactly one extra step, whatever the number of warnings. |

## 7. Edge Cases and Error Scenarios

| ID    | Scenario                                                                                       | Expected Behavior                                                                                                                  |
| ----- | ---------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------------------ |
| EC-1  | Employee submits with unsaved day edits still in the grid                                       | Warn that unsaved hours exist and offer to save first. Submission acts only on saved entries (spec 005 FR-013) — never on typed-but-unsaved ones |
| EC-2  | Every weekday is empty except one, and the week totals 2:00                                     | Both the empty-workday and short-week warnings fire. They are independent conditions and both are shown                              |
| EC-3  | Week total is exactly `expectedWeeklyMinutes`                                                   | No short-week warning. The test is strictly *below* the expected total                                                              |
| EC-4  | Week total is exactly `longWeekThresholdMinutes`                                                | No long-week warning. The test is strictly *above* the threshold                                                                     |
| EC-5  | Employee acknowledges warnings, then edits the week in another tab, then confirms the submission | Warnings are recomputed at confirmation (FR-009). If the set changed, the new set is shown and acknowledgement is asked again        |
| EC-6  | Submit is pressed twice in quick succession                                                     | The first submission wins. The second finds the week already Submitted and reports it as such, without a second `submittedAt`        |
| EC-7  | Employee withdraws at the same moment a manager approves                                        | Whichever transition commits first wins; the other is refused because its expected source state no longer holds. No week ends up in two states |
| EC-8  | Manager decides a week the employee withdrew a moment earlier                                   | Refused — the week is Draft and FR-034 allows a decision only from Submitted. The manager is told the week was withdrawn             |
| EC-9  | Two managers decide the same submitted week at once                                             | The optimistic check on `updatedAt` rejects the second. The decision that landed is shown, including who made it                     |
| EC-10 | Manager rejects with a comment of whitespace only                                               | Rejected as no comment at all (FR-034). Trim before validating, as in spec 001 EC-2                                                 |
| EC-11 | A project's budget is lowered after a week was submitted, putting it over budget                | Nothing changes about the week. The over-budget warning is evaluated only at submission, and the week's hours are already frozen     |
| EC-12 | Employee is unassigned from a project after submitting a week containing its hours              | The week is unaffected and stays submittable, reviewable and approvable. Spec 001 SC-014 already keeps the hours                     |
| EC-13 | The nudge run does not execute at all — host down, job stuck                                    | Occurrences already due are delivered on the next run (late, per NFR-006's breach), never skipped silently. Lateness is logged        |
| EC-14 | The nudge run executes twice for the same occurrence                                            | The uniqueness constraint makes the second a no-op (NFR-004). Nothing is delivered twice                                            |
| EC-15 | Employee's email address is invalid or bounces                                                  | The in-app nudge stands on its own (NFR-005). The email occurrence is marked failed, not delivered, and does not retry indefinitely  |
| EC-16 | A daylight-saving transition falls between a week and its deadline                              | Deadline and nudge moments are wall-clock times in the policy zone (NFR-009). 12:00 stays 12:00; no hour is gained or lost           |
| EC-17 | An employee joins mid-week and has assignments only from Wednesday                               | They are nudged (FR-025 tests for *any* assignment covering the week) and the empty-workday warning still fires for Monday and Tuesday. There is no partial-week expectation |
| EC-18 | A week from a year ago has never been submitted                                                 | It shows as Draft and overdue. Its five overdue nudges were long since exhausted, so it generates no new noise                       |
| EC-19 | Employee opens the week list having never used the app                                          | Show an empty state pointing at the current week's grid — not a table of twelve empty Draft rows                                     |
| EC-20 | A public holiday falls on a Thursday                                                            | The empty-workday warning fires anyway. There is no holiday calendar (§1.3); the employee acknowledges and submits                    |
| EC-21 | Employee dismisses an in-app nudge banner without submitting                                    | The banner stays dismissed for that occurrence only. The next scheduled occurrence raises a new one                                 |
| EC-22 | A week is rejected, edited, and resubmitted                                                     | `submittedAt` is overwritten, `acknowledgedWarnings` is recomputed, and the previous rejection comment stops being presented as current (SC-027). The week has one current state, not a history log |

## 8. Success Criteria

| ID     | Criterion                                                                                                                           |
| ------ | ------------------------------------------------------------------------------------------------------------------------------------- |
| SUC-01 | All 29 acceptance scenarios (SC-001 … SC-029) pass as automated tests in CI                                                           |
| SUC-02 | An employee with a complete week can submit it in a single action from the grid, with no confirmation step (FR-016, NFR-012)          |
| SUC-03 | An automated test proves every transition outside the five in §5.4 is refused, from all four source states                             |
| SUC-04 | An automated test proves no write to a time entry or week row succeeds while its week is Submitted or Approved (FR-005, NFR-007)      |
| SUC-05 | An automated test proves an employee can neither submit, withdraw, nor decide another person's week, and cannot decide their own (FR-006, §5.4, NFR-007) |
| SUC-06 | Running nudge delivery twice over the same window delivers each occurrence exactly once (FR-022, NFR-004)                              |
| SUC-07 | A week left unsubmitted for a month generates exactly seven nudges: one Friday, one deadline-day and five overdue (FR-017–FR-019)      |
| SUC-08 | Every TimesheetWeek that is Approved or Rejected has a decider and a decision time, and every Rejected one has a non-empty comment — verifiable as a data invariant at any point in time |
| SUC-09 | For a seeded person with 52 weeks of entries, the week list meets NFR-002 and every week's total matches the exact sum of its entries  |
| SUC-10 | Nothing in the submit, withdraw, approve or reject paths writes to TimeEntry or WeekRow — verifiable by test                           |

## 9. Dependencies and Constraints

### 9.1 Dependencies

- **[Spec 005](005-weekly-time-grid.md) — hard dependency.** TimeEntry and WeekRow must exist
  before a week can have a state worth submitting. This slice cannot ship or be demoed first.
- **The amendment to spec 005** (§5.4) must land with this slice, or Submitted weeks stay editable
  and submission means nothing.
- **[Spec 001](001-project-setup.md)** for Person and the Manager role (FR-034), Assignment
  (FR-025) and project budget state (FR-013).
- **Authentication and current-user resolution** — FR-006, FR-034 and NFR-007 all need to know who
  is acting and in what role. Still uncovered by any story in the map; inherited from spec 001 §10
  Q1 and spec 005 §10 Q1. Until it exists, the employee-side rules can be built against a stubbed
  current user, but the manager-side decision path cannot be verified.
- **A scheduled background runner** — FR-017 through FR-019 need something that wakes up on a
  schedule while nobody is using the app. Nothing in the project has this yet; it is new
  infrastructure, it affects deployment (arc42 §7), and it needs an ADR.
- **An email channel** — FR-024 only. In-app nudges (FR-023) have no such dependency, which is why
  FR-023 is Must and FR-024 is Should: the feature works with email switched off entirely.
- **Story 016 (approve / reject)** consumes the transitions in FR-034 and the fields they write. It
  must not extend the state machine.
- **Story 014 (who is late)** consumes the derived deadline and overdue rules (§5.4) across people.
- **Story 020 (export approved hours)** consumes the `Approved` state defined here. It is the reason
  Approved must be terminal until a reopen story exists.
- **Story 009 (copy last week)** copies *entries*, never status. A copied-into week is Draft like
  any other, and copying into a Submitted or Approved week is refused by FR-005.

### 9.2 Constraints

- ASP.NET Core Razor Pages on `net10.0`, nullable and implicit usings enabled.
- New C# files must use `namespace my_project.*` — the project's `RootNamespace` is `my_project`
  (underscore) while the directory and assembly are `my-project`.
- No test project exists yet. SUC-01 requires creating a sibling project (e.g. `my-project.Tests`)
  and a `.sln`.
- This is the first feature in the project that does work **without a user present**. Whatever
  hosts the nudge schedule — a hosted service in-process, an external scheduler, something else —
  becomes the project's precedent for background work and must be recorded as an ADR.
- The policy time zone (§5.3) makes this the first feature with a wall-clock dependency. The
  existing model is deliberately instant-free (spec 005 EC-18); that stops being true here, and the
  boundary between calendar dates and instants must be explicit in the code.

### 9.3 Architecture References

All arc42 sections below are currently unpopulated placeholder templates.

| Arc42 Section                    | Relevance to This Feature                                                                                                     |
| -------------------------------- | -------------------------------------------------------------------------------------------------------------------------------- |
| 3. Context & Scope               | Adds the first outbound external interface — email (FR-024) — and the first actor that is a clock rather than a person             |
| 5. Building Block View           | Adds TimesheetWeek and SubmissionNudge, and a scheduled nudge runner alongside the web application                                 |
| 6. Runtime View                  | Two flows worth diagramming: submit-with-warnings (evaluate, acknowledge, transition) and the nudge run (select, suppress, deliver) |
| 7. Deployment View               | First background process in the system — where the schedule runs, and what happens if the host restarts, is a deployment concern    |
| 8. Crosscutting Concepts         | Sets the project's patterns for state machines, optimistic concurrency on transitions, scheduled work and outbound notification      |
| 9. Architecture Decisions (ADRs) | Needs ADRs for background scheduling, for the policy time zone and wall-clock handling, and for organisation-wide policy as configuration rather than data |
| 10. Quality Requirements         | NFR-004 (idempotent delivery) and NFR-009 (one time zone) are strong candidates for promotion to project-wide quality scenarios      |
| 12. Glossary                     | Week status, draft, submitted, approved, rejected, withdrawal, submission deadline, overdue, nudge should be entered here            |

## 10. Open Questions

| #   | Question                                                                                                              | Owner | Status | Resolution                                                                                                                          |
| --- | ----------------------------------------------------------------------------------------------------------------------- | ----- | ------ | -------------------------------------------------------------------------------------------------------------------------------------- |
| 1   | How is the current user established and signed in? FR-006, FR-034 and NFR-007 depend on it.                              | stijn | Open   | Inherited from spec 001 §10 Q1 and spec 005 §10 Q1. Needs a new story in the map, or an explicit decision to stub the current user.     |
| 2   | Should expected weekly hours be per person rather than organisation-wide? Part-timers get a short-week warning every week. | stijn | Open   | Assumed organisation-wide at 40:00 (§5.3). Moving it to Person amends spec 001 §5.1 and changes only the FR-011 test — deliberately deferred. |
| 3   | Can an approved week ever be corrected, and who may reopen it?                                                          | stijn | Open   | Assumed terminal (§5.4). Needs a decision with story 016 or 020 before the first real payroll export, since export is what makes it hurt. |

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
