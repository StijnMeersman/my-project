using my_project.Domain;

namespace my_project.Application;

/// <summary>One row of the project list (spec 001 FR-013).</summary>
public sealed record ProjectListItem(
    Guid Id,
    string Name,
    string ClientName,
    BudgetPosition Budget,
    int AssignedPeopleCount);

/// <summary>Someone currently assigned to a project, with what they have logged against it.</summary>
/// <param name="LoggedMinutes">Drives the FR-011 warning: unassigning is only consequential when this is above zero.</param>
public sealed record AssignedPerson(
    Guid PersonId,
    string FullName,
    string Email,
    PersonRole Role,
    int LoggedMinutes);

/// <summary>Everything one project's page shows (spec 001 FR-012, FR-013).</summary>
public sealed record ProjectDetail(
    Guid Id,
    string Name,
    Guid ClientId,
    string ClientName,
    BudgetPosition Budget,
    IReadOnlyList<AssignedPerson> AssignedPeople,
    IReadOnlyList<Person> AssignablePeople);
