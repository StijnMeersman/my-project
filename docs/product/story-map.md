# User Story Map: Time Registration

## Goal
Make logging hours against projects so fast and forgiving that nobody reconstructs their week from memory on Friday — and give managers a clear, trustworthy approval flow so payroll and billing go out correct the first time.

## Primary User
Two sides of one flow:

- **The employee** — logs hours per day against the projects they are assigned to, and submits their week for approval. They want to be done in seconds and never be the person holding up the deadline.
- **The manager** — sets up projects with a fixed hour budget, decides who may work on them, and reviews and approves submitted weeks. They want mistakes surfaced before the numbers leave the building.

## Key rules that shape the app
- Hours are always logged **against a specific project** — never free-floating.
- Every project has a **predefined hour budget** set by the manager up front. Budget vs. burned is visible throughout, not just in reporting.
- Employees only see the projects they have been **assigned** to.
- Entry granularity is **per day, per project**. The **week** is the submission and approval unit.

## Story Map

### Set up projects & people
| #   | Story                                                                                            |
| --- | ------------------------------------------------------------------------------------------------ |
| 001 | As a manager, I create a project with a client and a predefined budget of hours                   |
| 002 | As a manager, I assign team members to a project so only they can log hours against it            |
| 003 | As a manager, I see all my projects in one list with their budget and hours burned so far         |
| 004 | As a manager, I close or archive a finished project so it stops appearing on timesheets           |

### Log hours
| #   | Story                                                                                            |
| --- | ------------------------------------------------------------------------------------------------ |
| 005 | As an employee, I see my week as a grid with my assigned projects as rows and days as columns     |
| 006 | As an employee, I enter hours on a specific day and project, with an optional note                |
| 007 | As an employee, I see a project's remaining budget while I am logging against it                  |
| 008 | As an employee, I see my running total per day and for the whole week                             |
| 009 | As an employee, I copy last week's entries into this week so I can fill it in seconds             |

### Submit the week
| #   | Story                                                                                            |
| --- | ------------------------------------------------------------------------------------------------ |
| 010 | As an employee, I submit my week for approval when I am done                                      |
| 011 | As an employee, I am warned before submitting if a workday is empty or my week is short on hours  |
| 012 | As an employee, I get nudged before the submission deadline so I never forget my week             |
| 013 | As an employee, I see the status of each of my weeks: draft, submitted, approved or rejected      |

### Review & approve
| #   | Story                                                                                            |
| --- | ------------------------------------------------------------------------------------------------ |
| 014 | As a manager, I see who has submitted their week and who is still late                            |
| 015 | As a manager, I open a person's week and see every entry they logged, per day and per project     |
| 016 | As a manager, I approve or reject a submitted week, with a comment explaining a rejection         |
| 017 | As a manager, I get suspicious entries flagged for me, such as hours over budget or odd totals    |

### See where hours went
| #   | Story                                                                                            |
| --- | ------------------------------------------------------------------------------------------------ |
| 018 | As a manager, I open a project dashboard showing budget versus hours burned                       |
| 019 | As a manager, I break hours down per person and per project to see where the time actually goes   |
| 020 | As a manager, I export approved hours so they can be used for payroll and billing                 |

---

## Suggested starting points
Not a release plan — just where the app comes alive fastest:

- **The spine:** 001 → 002 → 005 → 006. Once a manager can create a project with a budget, assign someone, and that person can fill a weekly grid, you have a working product.
- **The promise:** 009, 011 and 012 are the stories that deliver the actual goal ("logging is not a chore"). Do not leave them for last.
- **The close of the loop:** 010 → 014 → 016 makes the two-sided flow real end to end.
