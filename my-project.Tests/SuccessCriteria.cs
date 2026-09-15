using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using my_project.Application;
using my_project.Domain;

namespace my_project.Tests;

/// <summary>
/// The measurable claims of <see href="../docs/specs/001-project-setup.md">spec 001 §8</see>.
/// SUC-01 is the whole of <see cref="AcceptanceScenarios"/>; SUC-02 is a stopwatch held by a human
/// and cannot be asserted here.
/// </summary>
public class SuccessCriteria
{
    [Fact(DisplayName = "SUC-03: 1 000 entries sum to the exact arithmetic total, with no drift")]
    public async Task SUC03_ThousandEntriesSumExactly()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Long haul", client.Id, "2000");
        var person = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");

        // Deliberately awkward lengths: 7 minutes, 23 minutes, 1 minute… the sort of values that
        // accumulate visible error the moment hours are held as floating point.
        var minutes = Enumerable.Range(0, 1_000).Select(i => 1 + (i * 7 % 83)).ToArray();
        var expected = minutes.Sum();

        await SeedEntriesAsync(app, project.Id, person.Id, minutes);

        var row = await app.ProjectRowAsync("Long haul");

        Assert.Equal(expected, row.Budget.BurnedMinutes);
        Assert.Equal((2000 * 60) - expected, row.Budget.RemainingMinutes);
    }

    [Fact(DisplayName = "SUC-04: An Employee cannot create or modify anything in this slice")]
    public async Task SUC04_EmployeeCannotChangeAnything()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website", client.Id, "100");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");

        var employee = app.AsEmployee;

        await Assert.ThrowsAsync<ForbiddenException>(() => employee.Clients.CreateAsync("Globex"));
        await Assert.ThrowsAsync<ForbiddenException>(() => employee.People.CreateAsync("Kit Lowe", "kit@acme.test", PersonRole.Employee));
        await Assert.ThrowsAsync<ForbiddenException>(() => employee.Projects.CreateAsync("Portal", client.Id, "50"));
        await Assert.ThrowsAsync<ForbiddenException>(() => employee.Projects.UpdateAsync(project.Id, "Renamed", client.Id, "999"));
        await Assert.ThrowsAsync<ForbiddenException>(() => employee.Assignments.AssignAsync(project.Id, [sam.Id]));
        await Assert.ThrowsAsync<ForbiddenException>(() => employee.Assignments.UnassignAsync(project.Id, sam.Id, confirmed: true));

        // Reading is not changing (FR-018): the employee still sees the list they are refused edits on.
        Assert.Single(await employee.Projects.ListAsync());

        // And the database is exactly as the manager left it.
        Assert.Single(await app.Clients.ListAsync());
        Assert.Single(await app.People.ListAsync());
        Assert.Equal("Website", (await app.ProjectRowAsync("Website")).Name);
        Assert.Equal(100 * 60, (await app.ProjectRowAsync("Website")).Budget.BudgetMinutes);
        Assert.Empty(await app.Db.Assignments.ToListAsync());
    }

    [Fact(DisplayName = "SUC-05/NFR-001: 200 projects and 50 000 entries list in one query, under 500 ms")]
    public async Task SUC05_ProjectListScales()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");

        var projectIds = new List<Guid>();
        for (var p = 0; p < 200; p++)
        {
            projectIds.Add((await app.GivenProjectAsync($"Project {p:000}", client.Id, "500")).Id);
        }

        var personIds = new List<Guid>();
        for (var i = 0; i < 50; i++)
        {
            personIds.Add((await app.GivenPersonAsync($"Person {i:00}", $"person{i:00}@acme.test")).Id);
        }

        // 200 × 50 × 5 = 50 000 entries, each on a distinct (person, project, date) as the index demands.
        await SeedGridAsync(app, projectIds, personIds, daysPerPair: 5);
        Assert.Equal(50_000, await app.Db.TimeEntries.CountAsync());

        // Warm up: the first call pays for EF's model and query compilation, which no user waits for
        // more than once per process.
        await app.Projects.ListAsync();

        app.Commands.Reset();
        var stopwatch = Stopwatch.StartNew();
        var rows = await app.Projects.ListAsync();
        stopwatch.Stop();

        Assert.Equal(200, rows.Count);

        // The half of NFR-001 that does not depend on the machine: one statement for the whole page,
        // not one per project.
        Assert.Equal(1, app.Commands.Count);
        Assert.True(
            stopwatch.ElapsedMilliseconds < 500,
            $"The project list took {stopwatch.ElapsedMilliseconds} ms; NFR-001 allows 500 ms.");

        // Each project holds 50 people × 5 days × 60 minutes.
        Assert.All(rows, r => Assert.Equal(50 * 5 * 60, r.Budget.BurnedMinutes));
    }

    [Fact(DisplayName = "SUC-06: Every project has exactly one client and a budget above zero")]
    public async Task SUC06_ProjectInvariantHolds()
    {
        using var app = new ProjectSetupFixture();
        var acme = await app.GivenClientAsync("Acme Corp");
        var globex = await app.GivenClientAsync("Globex");
        await app.GivenProjectAsync("Website", acme.Id, "120");
        var support = await app.GivenProjectAsync("Support", globex.Id, "40");

        // Everything below is an attempt to leave a project without a client or without a budget.
        await app.Projects.CreateAsync("No client", Guid.Empty, "10");
        await app.Projects.CreateAsync("Missing client", Guid.NewGuid(), "10");
        await app.Projects.CreateAsync("No budget", acme.Id, "0");
        await app.Projects.CreateAsync("Negative budget", acme.Id, "-5");
        await app.Projects.UpdateAsync(support.Id, "Support", Guid.Empty, "40");
        await app.Projects.UpdateAsync(support.Id, "Support", globex.Id, "0");

        var projects = await app.Db.Projects.AsNoTracking().ToListAsync();
        var clientIds = await app.Db.Clients.Select(c => c.Id).ToListAsync();

        Assert.Equal(2, projects.Count);
        Assert.All(projects, p =>
        {
            Assert.Contains(p.ClientId, clientIds);
            Assert.True(p.BudgetMinutes > 0, $"\"{p.Name}\" has a budget of {p.BudgetMinutes} minutes.");
        });
    }

    /// <summary>
    /// Writes entries straight to the database. Logging hours is story 006's job — spec 001 only ever
    /// reads them — so going through a service here would be inventing one.
    /// </summary>
    private static async Task SeedEntriesAsync(
        ProjectSetupFixture app,
        Guid projectId,
        Guid personId,
        IReadOnlyList<int> minutes)
    {
        var now = app.Clock.GetUtcNow();
        var start = new DateOnly(2020, 1, 1);

        app.Db.ChangeTracker.AutoDetectChangesEnabled = false;
        for (var i = 0; i < minutes.Count; i++)
        {
            // One entry per day keeps (personId, projectId, date) unique.
            app.Db.TimeEntries.Add(TimeEntry.Create(
                personId, projectId, start.AddDays(i), Duration.FromMinutes(minutes[i]), note: null, now));
        }

        await app.Db.SaveChangesAsync();
        app.Db.ChangeTracker.AutoDetectChangesEnabled = true;
        app.Db.ChangeTracker.Clear();
    }

    private static async Task SeedGridAsync(
        ProjectSetupFixture app,
        IReadOnlyList<Guid> projectIds,
        IReadOnlyList<Guid> personIds,
        int daysPerPair)
    {
        var now = app.Clock.GetUtcNow();
        var start = new DateOnly(2026, 1, 5);
        var hour = Duration.FromMinutes(60);

        app.Db.ChangeTracker.AutoDetectChangesEnabled = false;

        foreach (var projectId in projectIds)
        {
            foreach (var personId in personIds)
            {
                for (var d = 0; d < daysPerPair; d++)
                {
                    app.Db.TimeEntries.Add(
                        TimeEntry.Create(personId, projectId, start.AddDays(d), hour, note: null, now));
                }
            }

            // Save per project so the change tracker never holds all 50 000 at once.
            await app.Db.SaveChangesAsync();
            app.Db.ChangeTracker.Clear();
        }

        app.Db.ChangeTracker.AutoDetectChangesEnabled = true;
    }
}
