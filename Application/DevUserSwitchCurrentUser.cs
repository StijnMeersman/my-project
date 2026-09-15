using Microsoft.EntityFrameworkCore;
using my_project.Data;
using my_project.Domain;

namespace my_project.Application;

/// <summary>
/// A stand-in for authentication: the acting person is read from a cookie that the layout's switcher
/// sets, and the role comes from that person's own record. It exists so the Manager-only behaviour of
/// spec 001 FR-018 and the own-week rule of spec 005 FR-022 are demonstrable and testable before the
/// authentication story exists — see spec 001 §10 Q1 and ADR-0004.
/// <para>
/// <b>Not for production.</b> Anyone can set the cookie and become anyone. Replace this registration
/// with a real identity-backed implementation when auth lands; nothing else needs to change.
/// </para>
/// </summary>
public sealed class DevUserSwitchCurrentUser(
    IHttpContextAccessor httpContextAccessor,
    TimeRegistrationDbContext db) : ICurrentUser
{
    public const string CookieName = "dev-person";

    private bool _resolved;
    private Person? _person;

    public Guid? PersonId => Current?.Id;

    /// <summary>Manager when there is nobody at all, so an empty database can still be set up.</summary>
    public PersonRole Role => Current?.Role ?? PersonRole.Manager;

    public string DisplayName => Current?.FullName ?? "Nobody yet";

    /// <summary>
    /// Resolved once per request and cached. The query is synchronous because <see cref="ICurrentUser"/>
    /// is read from property getters all over the app — a cost this stub can afford and its
    /// replacement will not pay, since real identity reads claims rather than the database.
    /// </summary>
    private Person? Current
    {
        get
        {
            if (_resolved)
            {
                return _person;
            }

            _resolved = true;

            var cookie = httpContextAccessor.HttpContext?.Request.Cookies[CookieName];
            if (Guid.TryParse(cookie, out var id))
            {
                _person = db.People.AsNoTracking().FirstOrDefault(p => p.Id == id);
            }

            // Nobody picked, or the pick names someone who no longer exists: fall back to a manager,
            // which is what the role-only version of this stub defaulted to.
            _person ??= db.People.AsNoTracking().FirstOrDefault(p => p.Role == PersonRole.Manager)
                ?? db.People.AsNoTracking().OrderBy(p => p.FullName).FirstOrDefault();

            return _person;
        }
    }
}

/// <summary>A fixed person and role, for tests and for any caller that already knows who is acting.</summary>
public sealed class FixedCurrentUser(
    PersonRole role,
    string displayName = "Test user",
    Guid? personId = null) : ICurrentUser
{
    public PersonRole Role { get; } = role;

    public string DisplayName { get; } = displayName;

    public Guid? PersonId { get; } = personId;
}
