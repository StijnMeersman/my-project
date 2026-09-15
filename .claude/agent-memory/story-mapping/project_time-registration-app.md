---
name: project-time-registration-app
description: The my-project app is a team time-registration tool — employees log hours per day per project, managers set project hour budgets and approve weekly timesheets
metadata:
  type: project
---

`my-project` is being built as a **team time-registration app with an approval flow**. Story map lives at `docs/product/story-map.md` (stories 001–020, five activities).

**Why:** The user's stated pain is that *logging time is a chore* — people forget during the week and reconstruct it from memory on Friday, which makes payroll and billing wrong. The app's whole personality is "entry is fast, forgiving, and nudges you before the week is gone." The manager-side approval exists to catch mistakes *before* numbers go out, not to police people.

**Domain rules the user was explicit about (these constrain every story):**
- Hours are always logged against a **specific project** — never free-floating time.
- Every project has a **predefined hour budget set by the manager up front**. Budget-vs-burned should stay visible during logging and approving, not only in reporting.
- Managers **assign** people to projects; employees only see their own projects.
- Entry granularity is **per day, per project**. The **week** is the submission/approval unit.

**How to apply:** When writing specs for stories 001–020, treat the four rules above as given — do not re-litigate them. If a proposed feature makes logging slower or adds steps before submitting, it works against the project's core goal and should be questioned. See [[persona-employee-and-manager]] and [[decision-story-map-scope]].
