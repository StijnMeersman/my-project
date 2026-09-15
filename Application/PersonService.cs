using Microsoft.EntityFrameworkCore;
using my_project.Common;
using my_project.Data;
using my_project.Domain;

namespace my_project.Application;

/// <summary>Adding the people who can be assigned to projects (spec 001 US-002).</summary>
public sealed class PersonService(
    TimeRegistrationDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<Person>> ListAsync(CancellationToken ct = default) =>
        await db.People.AsNoTracking().OrderBy(p => p.FullName).ToListAsync(ct);

    /// <summary>FR-003, FR-004. Rejects an email that duplicates an existing person, case-insensitively.</summary>
    public async Task<Result<Person>> CreateAsync(
        string? fullName,
        string? email,
        PersonRole role,
        CancellationToken ct = default)
    {
        currentUser.RequireManager();

        var created = Person.Create(fullName, email, role, clock.GetUtcNow());
        if (!created.IsSuccess)
        {
            return created;
        }

        var person = created.Value;

        if (await db.People.AnyAsync(p => p.NormalizedEmail == person.NormalizedEmail, ct))
        {
            return DuplicateEmail(person.Email.Value);
        }

        db.People.Add(person);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            db.Entry(person).State = EntityState.Detached;
            return DuplicateEmail(person.Email.Value);
        }

        return Result<Person>.Ok(person);
    }

    private static Result<Person> DuplicateEmail(string email) =>
        Result<Person>.Fail(nameof(Person.Email), $"The email address \"{email}\" is already in use.");
}
