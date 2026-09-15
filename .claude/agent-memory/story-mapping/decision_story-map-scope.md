---
name: decision-story-map-scope
description: Scope rationale for the 20-story time-registration map — what was included, what was deliberately left out, and the user's appetite for scope
metadata:
  type: project
---

The story map was scoped to **20 stories across 5 activities** and the user approved it unchanged ("Ship it") when offered the chance to trim.

**Included deliberately:**
- Copy-last-week (009), pre-submit warnings (011), deadline nudges (012) — these are *not* nice-to-haves. They are the stories that deliver the stated goal of making logging painless. Cutting them guts the product.
- Flagging suspicious entries (017) and export for payroll/billing (020) — these make the approval loop feel finished rather than a toy.

**Deliberately not in the map:**
- Start/stop timers and live time tracking. The user's model is retrospective per-day entry, not stopwatch tracking.
- Invoicing itself. The app exports approved hours (020); generating invoices is out of scope.
- Per-task or per-ticket granularity beneath a project. Granularity stops at day + project.
- Release slicing / MVP phases. Story maps here list numbered stories only; the map ends with "suggested starting points" instead.

**Why:** The user wanted an ambitious-but-buildable app and explicitly declined the "trim it" option, so scope appetite is healthy — do not pre-emptively shrink future proposals for this project.

**How to apply:** If asked to expand the map later, add stories in the direction of *faster entry* and *richer budget insight* first. See [[project-time-registration-app]].
