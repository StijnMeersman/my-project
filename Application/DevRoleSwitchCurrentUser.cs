using my_project.Domain;

namespace my_project.Application;

/// <summary>
/// A stand-in for authentication: the role is read from a cookie that the layout's role switcher
/// sets. It exists so the Manager-only behaviour of FR-018 (and SC-019) is demonstrable and
/// testable before the authentication story exists — see spec 001 §10 Q1.
/// <para>
/// <b>Not for production.</b> Anyone can set the cookie. Replace this registration with a real
/// identity-backed implementation when auth lands; nothing else needs to change.
/// </para>
/// </summary>
public sealed class DevRoleSwitchCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public const string CookieName = "dev-role";

    public PersonRole Role =>
        Enum.TryParse<PersonRole>(httpContextAccessor.HttpContext?.Request.Cookies[CookieName], out var role)
            ? role
            : PersonRole.Manager;

    public string DisplayName => Role == PersonRole.Manager ? "Demo manager" : "Demo employee";
}

/// <summary>A fixed role, for tests and for any caller that already knows who is acting.</summary>
public sealed class FixedCurrentUser(PersonRole role, string displayName = "Test user") : ICurrentUser
{
    public PersonRole Role { get; } = role;

    public string DisplayName { get; } = displayName;
}
