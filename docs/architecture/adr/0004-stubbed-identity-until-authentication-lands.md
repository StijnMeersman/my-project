# ADR-0004: Identity is a stub behind `ICurrentUser` until authentication lands

| | |
| --- | --- |
| **Status** | Accepted — temporary, to be superseded |
| **Date** | 2026-09-15 |
| **Deciders** | Stijn |
| **Context** | [Spec 001](../../specs/001-project-setup.md) FR-018, NFR-002, §10 Q1 |

## Context

Spec 001 restricts creating and changing clients, people, projects and assignments to the Manager
role (FR-018), and NFR-002 insists the restriction is enforced server-side on every action rather
than by hiding buttons. SC-019 makes it an acceptance scenario.

But spec 001 §10 Q1 — how people authenticate — is still open, and no story in the map covers login.
The rule has to be built and demonstrated before there is anyone to authenticate.

## Decision

Put the whole question behind a one-method abstraction and stub the implementation.

- `ICurrentUser` exposes `DisplayName`, `Role` and `IsManager`. Nothing else in the codebase asks who
  the user is.
- `DevRoleSwitchCurrentUser` reads the role from a `dev-role` cookie set by a switcher in the layout,
  defaulting to Manager. It is marked *not for production* in its own summary.
- Tests use `FixedCurrentUser(role)` instead, so SC-019 and SUC-04 attack the same database a manager
  just populated.
- Enforcement is **two layers**, because the spec asks for actions to be guarded, not pages:
  - `[ManagerOnly]` page filter on the manager-only Razor Pages, and
  - `currentUser.RequireManager()` at the top of every service write, throwing `ForbiddenException`.

  A filter only covers requests that route through a page. The service call is the one thing every
  write has in common, so that is where the rule actually lives; the filter exists to produce a
  decent 403 page rather than an exception.
- Read paths are deliberately *not* gated. FR-018 restricts changing, not looking — `/Projects` is
  readable by an employee, and SUC-04 asserts that.

## Consequences

**Good**

- FR-018 is real code, tested against a real database, before authentication exists. Swapping in a
  genuine implementation means changing one DI registration in `Program.cs`.
- Manager-versus-Employee behaviour is demoable: flip the switcher in the nav and the writes start
  returning 403.

**Bad / accepted**

- **Anyone can become a manager by setting a cookie.** This is not access control; it is a
  placeholder shaped like access control. The app must not be exposed to untrusted users in this
  state. Logged in `11-risks-and-technical-debt.md`.
- `ManagerOnlyAttribute` returns a bare `403` rather than `ForbidResult`, because no authentication
  scheme is registered to hand a challenge to. That line changes when auth lands.
- `PersonRole` currently lives on `Person` and is unrelated to the stub's notion of the current user —
  there is no link between "the signed-in user" and a row in `People` yet. Establishing it is part of
  the work that supersedes this ADR.

## Alternatives considered

| Option | Why not |
| --- | --- |
| **Build authentication now** | §10 Q1 is unanswered and no story covers it. Guessing at a scheme would be a larger, riskier decision made with less information than a later one. |
| **Hard-code Manager everywhere** | FR-018 and SC-019 would be untestable and the restriction would exist only as a comment. |
| **ASP.NET Core Identity with a seeded user** | Real users, real password storage, real migrations — a substantial commitment to an answer nobody has chosen yet, for a slice that does not need it. |
