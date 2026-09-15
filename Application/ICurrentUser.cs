using my_project.Domain;

namespace my_project.Application;

/// <summary>
/// Who is acting: which <see cref="Person"/> they are, and what they are allowed to set up
/// (spec 001 FR-018, spec 005 FR-022).
/// <para>
/// Spec 001 §10 Q1 is still open: no story covers authentication, so there is nothing to resolve a
/// real signed-in user from yet. Everything that depends on identity goes through this interface so
/// that swapping <see cref="DevUserSwitchCurrentUser"/> for real auth is a one-line registration
/// change and touches nothing else.
/// </para>
/// </summary>
public interface ICurrentUser
{
    string DisplayName { get; }

    PersonRole Role { get; }

    /// <summary>
    /// The row in <c>People</c> this user <em>is</em>, or null when nobody has been picked yet.
    /// <para>
    /// Spec 001 needed only a role — it never asked whose data was being looked at. Spec 005 does:
    /// FR-022 restricts an employee to their own week, and "own" is meaningless without this link.
    /// See <see href="../docs/architecture/adr/0004-stubbed-identity-until-authentication-lands.md">ADR-0004</see>.
    /// </para>
    /// </summary>
    Guid? PersonId { get; }

    bool IsManager => Role == PersonRole.Manager;
}

/// <summary>
/// Thrown when someone attempts an action their role does not allow. Distinct from a validation
/// failure: nothing about the input was wrong, the actor was.
/// </summary>
public sealed class ForbiddenException(string message) : Exception(message);

public static class CurrentUserExtensions
{
    /// <summary>
    /// The server-side half of FR-018. Hiding a button is not enforcement (NFR-002), so every write
    /// in this slice passes through here regardless of what the UI offered.
    /// </summary>
    public static void RequireManager(this ICurrentUser user)
    {
        if (!user.IsManager)
        {
            throw new ForbiddenException("Only a manager can change clients, people, projects and assignments.");
        }
    }

    /// <summary>
    /// The server-side half of spec 005 FR-022 and NFR-005. A timesheet is always <em>this</em>
    /// person's, never one named by the request — which is what makes SC-024 unreachable rather than
    /// merely refused.
    /// </summary>
    public static Guid RequirePerson(this ICurrentUser user) =>
        user.PersonId
        ?? throw new ForbiddenException("Hours belong to a person, and there is nobody signed in to own them.");
}
