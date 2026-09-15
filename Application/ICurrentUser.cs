using my_project.Domain;

namespace my_project.Application;

/// <summary>
/// Who is acting, and what they are allowed to set up (spec 001 FR-018).
/// <para>
/// Spec 001 §10 Q1 is still open: no story covers authentication, so there is nothing to resolve a
/// real signed-in user from yet. Everything that depends on identity goes through this interface so
/// that swapping <see cref="DevRoleSwitchCurrentUser"/> for real auth is a one-line registration
/// change and touches nothing else.
/// </para>
/// </summary>
public interface ICurrentUser
{
    string DisplayName { get; }

    PersonRole Role { get; }

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
}
