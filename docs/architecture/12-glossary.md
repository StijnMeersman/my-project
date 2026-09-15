# 12. Glossary

<!-- Domain and technical terms used throughout the documentation -->

| Term | Definition |
| ---- | ---------- |
| **Client** | An organisation work is done for. Has a name, unique across the system regardless of case or surrounding whitespace. A client may have any number of projects, including none. |
| **Project** | A funded container for work, belonging to **exactly one** client and carrying a budget greater than zero. Every logged hour belongs to exactly one project. Project names are unique *per client*, not globally — two clients may both have a "Website Redesign". |
| **Person** | Someone who can be assigned to projects and log hours. Has a full name, a unique email address, and a role of Employee or Manager. |
| **Manager** | The role permitted to create and change clients, people, projects and assignments. Everyone can *read* the project list; only a manager can change what is on it. |
| **Assignment** | The link saying a person works on a project. A person is assigned to a project at most once. Removing an assignment stops future logging; it does not remove or discount hours already logged. |
| **Budget hours** | The hours planned for a project, set when it is created and editable afterwards. A plan, not a limit: nothing prevents burned hours from exceeding it. |
| **Burned hours** | The sum of every time entry on a project, whatever the state of the timesheet week it belongs to and whether or not the person who logged it is still assigned. Computed from live data on every read and never stored — see [ADR-0002](adr/0002-burned-hours-derived-never-stored.md). |
| **Remaining hours** | Budget hours minus burned hours. Signed and never clamped: a project 100 hours over budget reads `-100:00`. |
| **Approaching budget** | A project whose burned hours have reached 90% of its budget without passing it. Shown as a warning on the project list. |
| **Over budget** | A project whose burned hours exceed its budget. A state to be shown, not an error to be prevented. |
| **Duration** | A length of time held as a whole number of minutes, rendered `hh:mm`. Accepts `120:30` or `120` on input; a value that does not land on a whole minute is refused rather than rounded — see [ADR-0003](adr/0003-durations-as-whole-minutes.md). |
| **Time entry** | Hours a person logged against a project on one date. Owned by [spec 005](../specs/005-weekly-time-grid.md); spec 001 only ever reads them, to compute burned hours. |
| **Timesheet week** | A person's week of entries, with a status of Draft, Submitted, Approved or Rejected. Owned by [spec 010](../specs/010-submit-week.md). Burned hours ignore this status entirely. |
| **Normalized name / email** | The uppercased, trimmed form of a name or email, persisted alongside the original and carrying the unique index. Uppercased with `ToUpperInvariant()` so a Turkish server reaches the same uniqueness verdict as a Dutch one. |
