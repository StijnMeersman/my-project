using Microsoft.EntityFrameworkCore;
using my_project.Common;
using my_project.Data;
using my_project.Domain;

namespace my_project.Application;

/// <summary>Registering the organisations work is done for (spec 001 US-001).</summary>
public sealed class ClientService(
    TimeRegistrationDbContext db,
    ICurrentUser currentUser,
    TimeProvider clock)
{
    public async Task<IReadOnlyList<Client>> ListAsync(CancellationToken ct = default) =>
        await db.Clients.AsNoTracking().OrderBy(c => c.Name).ToListAsync(ct);

    /// <summary>FR-001, FR-002. Rejects a name that duplicates an existing client, case-insensitively.</summary>
    public async Task<Result<Client>> CreateAsync(string? name, CancellationToken ct = default)
    {
        currentUser.RequireManager();

        var created = Client.Create(name, clock.GetUtcNow());
        if (!created.IsSuccess)
        {
            return created;
        }

        var client = created.Value;

        if (await db.Clients.AnyAsync(c => c.NormalizedName == client.NormalizedName, ct))
        {
            return DuplicateName(client.Name);
        }

        db.Clients.Add(client);

        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (UniqueIndexViolation.Caused(ex))
        {
            db.Entry(client).State = EntityState.Detached;
            return DuplicateName(client.Name);
        }

        return Result<Client>.Ok(client);
    }

    private static Result<Client> DuplicateName(string name) =>
        Result<Client>.Fail(nameof(Client.Name), $"A client named \"{name}\" already exists.");
}
