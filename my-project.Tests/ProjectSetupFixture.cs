using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using my_project.Application;
using my_project.Data;
using my_project.Domain;

namespace my_project.Tests;

/// <summary>
/// One test's worth of the application, on a real but throwaway SQLite database.
/// <para>
/// Real SQLite rather than the in-memory provider on purpose: every uniqueness rule in spec 001 is
/// enforced by a unique index, and the in-memory provider would not enforce a single one of them.
/// The connection is held open for the lifetime of the fixture because <c>:memory:</c> drops the
/// database the moment the last connection closes.
/// </para>
/// </summary>
public sealed class ProjectSetupFixture : IDisposable
{
    private readonly SqliteConnection _connection;

    public ProjectSetupFixture()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        Commands = new CommandCounter();

        var options = new DbContextOptionsBuilder<TimeRegistrationDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(Commands)
            .Options;

        Db = new TimeRegistrationDbContext(options);
        Db.Database.EnsureCreated();

        // A fixed clock, so createdAt/updatedAt assertions are about the code, not about timing.
        Clock = new FakeTimeProvider(new DateTimeOffset(2026, 9, 15, 9, 0, 0, TimeSpan.Zero));

        AsManager = Acting(PersonRole.Manager);
        AsEmployee = Acting(PersonRole.Employee);
    }

    public TimeRegistrationDbContext Db { get; }

    public FakeTimeProvider Clock { get; }

    /// <summary>Counts SQL round trips, so NFR-001's "not one query per project" can be asserted.</summary>
    public CommandCounter Commands { get; }

    /// <summary>The default actor: every setup action in this slice is a manager's (FR-018).</summary>
    public ActingAs AsManager { get; }

    /// <summary>The same database seen by someone who is not allowed to change it (SC-019).</summary>
    public ActingAs AsEmployee { get; }

    public ClientService Clients => AsManager.Clients;

    public PersonService People => AsManager.People;

    public ProjectService Projects => AsManager.Projects;

    public AssignmentService Assignments => AsManager.Assignments;

    private ActingAs Acting(PersonRole role)
    {
        var user = new FixedCurrentUser(role);
        return new ActingAs(
            new ClientService(Db, user, Clock),
            new PersonService(Db, user, Clock),
            new ProjectService(Db, user, Clock),
            new AssignmentService(Db, user, Clock));
    }

    public async Task<Client> GivenClientAsync(string name) =>
        (await Clients.CreateAsync(name)).Value;

    public async Task<Person> GivenPersonAsync(string fullName, string email, PersonRole role = PersonRole.Employee) =>
        (await People.CreateAsync(fullName, email, role)).Value;

    public async Task<Project> GivenProjectAsync(string name, Guid clientId, string budget) =>
        (await Projects.CreateAsync(name, clientId, budget)).Value;

    /// <summary>
    /// Seeds hours directly, bypassing the services: logging is story 006's job, and spec 001 only
    /// ever reads these (spec 001 §9.1). <paramref name="weekStatus"/> exists so SC-018 can prove
    /// that burned hours ignores it.
    /// </summary>
    public async Task GivenLoggedHoursAsync(
        Guid projectId,
        Guid personId,
        int minutes,
        TimesheetWeekStatus weekStatus = TimesheetWeekStatus.Draft,
        DateOnly? weekStart = null)
    {
        var start = weekStart ?? new DateOnly(2026, 9, 7);

        if (!await Db.TimesheetWeeks.AnyAsync(w => w.PersonId == personId && w.WeekStartDate == start))
        {
            Db.TimesheetWeeks.Add(TimesheetWeek.Create(personId, start, weekStatus, Clock.GetUtcNow()));
        }

        Db.TimeEntries.Add(TimeEntry.Create(
            personId,
            projectId,
            start,
            Duration.FromMinutes(minutes),
            note: null,
            Clock.GetUtcNow()));

        await Db.SaveChangesAsync();
    }

    public async Task<ProjectListItem> ProjectRowAsync(string name) =>
        (await Projects.ListAsync()).Single(p => p.Name == name);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

/// <summary>The four services, bound to one database and one actor.</summary>
public sealed record ActingAs(
    ClientService Clients,
    PersonService People,
    ProjectService Projects,
    AssignmentService Assignments);

/// <summary>
/// A tally of the SQL statements EF actually sent. NFR-001 is a claim about query shape as much as
/// about milliseconds, and query shape is the half that does not depend on the machine.
/// </summary>
public sealed class CommandCounter : DbCommandInterceptor
{
    public int Count { get; private set; }

    public void Reset() => Count = 0;

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result)
    {
        Count++;
        return base.ReaderExecuting(command, eventData, result);
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command,
        CommandEventData eventData,
        InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        Count++;
        return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
    }
}

/// <summary>A clock that does not move unless a test moves it.</summary>
public sealed class FakeTimeProvider(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}
