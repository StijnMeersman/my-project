using System.Globalization;
using Microsoft.EntityFrameworkCore;
using my_project.Application;
using my_project.Domain;

namespace my_project.Tests;

/// <summary>
/// The failure modes of <see href="../docs/specs/001-project-setup.md">spec 001 §7</see>.
/// EC-8 (last write wins) and EC-16 (no delete action exists) have nothing to assert: one is the
/// absence of a concurrency check, the other the absence of a feature.
/// </summary>
public class EdgeCases
{
    [Fact(DisplayName = "EC-1: An empty create form reports every field at once and persists nothing")]
    public async Task EC1_EmptyFormReportsEveryField()
    {
        using var app = new ProjectSetupFixture();

        var result = await app.Projects.CreateAsync(name: null, clientId: Guid.Empty, budget: null);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Field == nameof(Project.Name));
        Assert.Contains(result.Errors, e => e.Field == nameof(Project.ClientId));
        Assert.Contains(result.Errors, e => e.Field == nameof(Project.BudgetMinutes));
        Assert.Empty(await app.Projects.ListAsync());
    }

    [Theory(DisplayName = "EC-2: Names are trimmed, and a whitespace-only name is empty")]
    [InlineData("  Acme Corp  ", "Acme Corp")]
    [InlineData("Acme Corp", "Acme Corp")]
    public async Task EC2_NamesAreTrimmed(string input, string stored)
    {
        using var app = new ProjectSetupFixture();

        var result = await app.Clients.CreateAsync(input);

        Assert.True(result.IsSuccess);
        Assert.Equal(stored, result.Value.Name);
    }

    [Theory(DisplayName = "EC-2: A whitespace-only name is rejected as empty")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task EC2_WhitespaceOnlyNameIsRejected(string input)
    {
        using var app = new ProjectSetupFixture();

        var result = await app.Clients.CreateAsync(input);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("required"));
    }

    [Theory(DisplayName = "EC-3: A budget that is not a usable number is refused, never rounded")]
    [InlineData("abc")]
    [InlineData("-40")]
    [InlineData("120:70")]
    [InlineData("120.333")]      // 7 219.98 minutes — rejected rather than silently rounded
    [InlineData("1:2:3")]
    public async Task EC3_UnusableBudgetIsRefused(string budget)
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");

        var result = await app.Projects.CreateAsync("Website", client.Id, budget);

        Assert.False(result.IsSuccess);
        Assert.Empty(await app.Projects.ListAsync());
    }

    [Theory(DisplayName = "EC-3: A budget on a whole minute is accepted in either notation")]
    [InlineData("120", 7200)]
    [InlineData("120:30", 7230)]
    [InlineData("7.5", 450)]
    [InlineData("0.25", 15)]
    public async Task EC3_WholeMinuteBudgetsAreAccepted(string budget, int expectedMinutes)
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");

        var result = await app.Projects.CreateAsync("Website", client.Id, budget);

        Assert.True(result.IsSuccess);
        Assert.Equal(expectedMinutes, result.Value.BudgetMinutes);
    }

    [Fact(DisplayName = "EC-4: A budget above the 100 000-hour guard is refused")]
    public async Task EC4_BudgetAboveTheCapIsRefused()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");

        Assert.True((await app.Projects.CreateAsync("At the cap", client.Id, "100000")).IsSuccess);

        var result = await app.Projects.CreateAsync("Over the cap", client.Id, "100001");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("exceed"));
    }

    [Fact(DisplayName = "EC-5: A name longer than 200 characters is refused, not truncated")]
    public async Task EC5_OverlongNameIsRefused()
    {
        using var app = new ProjectSetupFixture();

        Assert.True((await app.Clients.CreateAsync(new string('a', 200))).IsSuccess);

        var result = await app.Clients.CreateAsync(new string('b', 201));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("200"));
        Assert.Single(await app.Clients.ListAsync());
    }

    [Theory(DisplayName = "EC-6: A malformed email is refused before the person is created")]
    [InlineData("sam")]
    [InlineData("sam@")]
    [InlineData("@acme.test")]
    [InlineData("sam acme.test")]
    [InlineData("sam@@acme.test")]
    public async Task EC6_MalformedEmailIsRefused(string email)
    {
        using var app = new ProjectSetupFixture();

        var result = await app.People.CreateAsync("Sam Rivers", email, PersonRole.Employee);

        Assert.False(result.IsSuccess);
        Assert.Empty(await app.People.ListAsync());
    }

    [Fact(DisplayName = "EC-7: Uniqueness reaches the same verdict on a Turkish server")]
    public async Task EC7_UniquenessIsCultureInvariant()
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            // Under tr-TR, "istanbul".ToUpper() is "İSTANBUL" — a culture-sensitive comparison would
            // let these two coexist on a Turkish server and not on a Dutch one.
            CultureInfo.CurrentCulture = new CultureInfo("tr-TR");

            using var app = new ProjectSetupFixture();
            await app.GivenClientAsync("ISTANBUL");

            var result = await app.Clients.CreateAsync("istanbul");

            Assert.False(result.IsSuccess);
            Assert.Single(await app.Clients.ListAsync());
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact(DisplayName = "EC-9: The database refuses a second assignment for the same pair")]
    public async Task EC9_ConcurrentAssignmentIsRefusedByTheIndex()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website", client.Id, "100");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.Assignments.AssignAsync(project.Id, [sam.Id]);

        // Writing behind the service's back is exactly what a second manager's request looks like.
        app.Db.Assignments.Add(Assignment.Create(project.Id, sam.Id, app.Clock.GetUtcNow()));
        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => app.Db.SaveChangesAsync());

        Assert.True(UniqueIndexViolation.Caused(ex));
    }

    [Fact(DisplayName = "EC-10: An empty project list is empty, not an error")]
    public async Task EC10_EmptyProjectList()
    {
        using var app = new ProjectSetupFixture();

        Assert.Empty(await app.Projects.ListAsync());
    }

    [Fact(DisplayName = "EC-11: A client with no projects is valid and still selectable")]
    public async Task EC11_ClientWithNoProjects()
    {
        using var app = new ProjectSetupFixture();
        var globex = await app.GivenClientAsync("Globex");

        Assert.Empty(await app.Projects.ListAsync());
        Assert.Contains(await app.Clients.ListAsync(), c => c.Id == globex.Id);
        Assert.True((await app.Projects.CreateAsync("First", globex.Id, "10")).IsSuccess);
    }

    [Fact(DisplayName = "EC-12: Unassigning someone with no logged hours needs no confirmation")]
    public async Task EC12_UnassigningWithNoHoursIsImmediate()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website", client.Id, "100");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.Assignments.AssignAsync(project.Id, [sam.Id]);

        var result = await app.Assignments.UnassignAsync(project.Id, sam.Id, confirmed: false);

        Assert.Equal(UnassignOutcome.Removed, result.Outcome);
        Assert.Equal(0, result.LoggedMinutes);
    }

    [Fact(DisplayName = "EC-13: A far overrun is shown in full, never clamped")]
    public async Task EC13_FarOverrunIsNotClamped()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Runaway", client.Id, "10");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, 110 * 60);

        var row = await app.ProjectRowAsync("Runaway");

        Assert.Equal(-100 * 60, row.Budget.RemainingMinutes);
        Assert.Equal("-100:00", Duration.ToHhMm(row.Budget.RemainingMinutes));
    }

    [Fact(DisplayName = "EC-14: Hours logged by someone since unassigned still count as burned")]
    public async Task EC14_HoursOfAnUnassignedPersonStillCount()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website", client.Id, "100");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.Assignments.AssignAsync(project.Id, [sam.Id]);
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, 20 * 60);
        await app.Assignments.UnassignAsync(project.Id, sam.Id, confirmed: true);

        var detail = await app.Projects.GetDetailAsync(project.Id);

        Assert.Equal(20 * 60, detail!.Budget.BurnedMinutes);
        Assert.DoesNotContain(detail.AssignedPeople, p => p.PersonId == sam.Id);
    }

    [Fact(DisplayName = "EC-15: Moving a project onto a client that already has that name is refused")]
    public async Task EC15_MovingOntoAConflictingClientIsRefused()
    {
        using var app = new ProjectSetupFixture();
        var acme = await app.GivenClientAsync("Acme Corp");
        var globex = await app.GivenClientAsync("Globex");
        var acmeSite = await app.GivenProjectAsync("Website", acme.Id, "100");
        await app.GivenProjectAsync("Website", globex.Id, "80");

        var result = await app.Projects.UpdateAsync(acmeSite.Id, "Website", globex.Id, "100");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("already has a project"));

        // The project is untouched — it still belongs to Acme.
        var reloaded = await app.Projects.GetDetailAsync(acmeSite.Id);
        Assert.Equal(acme.Id, reloaded!.ClientId);
    }
}
