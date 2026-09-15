---
name: persona-employee-and-manager
description: Two personas for the time-registration app — the employee who logs and submits a week, and the manager who sets project budgets and approves
metadata:
  type: project
---

The time-registration app has **two personas on one flow**, and nearly every story belongs clearly to one of them.

**The employee** — logs hours per day against projects they are assigned to, submits the week. Success for them is being done in seconds and never being the person who holds up the deadline. They are the persona the app's quality bar is set by.

**The manager** — creates projects with a fixed hour budget, assigns who may work on them, reviews and approves submitted weeks, and reads budget-vs-burned. Success for them is mistakes surfaced before payroll/billing goes out.

**Why:** The user chose "Team + approval" over freelancer / personal / pure-reporting framings, then immediately added that managers set hour budgets and assign people — so the manager is a genuine first-class user here, not an afterthought.

**How to apply:** Every spec should name which persona the story serves. When the two conflict (e.g. more manager control vs. faster employee entry), favour the employee — see [[project-time-registration-app]] for why speed of entry is the core promise.
