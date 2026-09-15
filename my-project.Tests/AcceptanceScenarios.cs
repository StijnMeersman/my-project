using Microsoft.EntityFrameworkCore;
using my_project.Application;
using my_project.Domain;

namespace my_project.Tests;

/// <summary>
/// The nineteen acceptance scenarios of
/// <see href="../docs/specs/001-project-setup.md">spec 001 §4</see>, one test each (SUC-01).
/// Each test's name is its scenario id, so a failure points straight back at the spec.
/// </summary>
public class AcceptanceScenarios
{
    [Fact(DisplayName = "SC-001: Register a client (FR-001)")]
    public async Task SC001_RegisterAClient()
    {
        using var app = new ProjectSetupFixture();

        var result = await app.Clients.CreateAsync("Acme Corp");

        Assert.True(result.IsSuccess);
        var clients = await app.Clients.ListAsync();
        Assert.Contains(clients, c => c.Name == "Acme Corp");

        // "And it is selectable when creating a project" — the only thing selectability means here.
        var project = await app.Projects.CreateAsync("Website Redesign", result.Value.Id, "120");
        Assert.True(project.IsSuccess);
    }

    [Fact(DisplayName = "SC-002: Duplicate client name is rejected (FR-002)")]
    public async Task SC002_DuplicateClientNameIsRejected()
    {
        using var app = new ProjectSetupFixture();
        await app.GivenClientAsync("Acme Corp");

        var result = await app.Clients.CreateAsync("ACME CORP");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("already exists"));
        Assert.Single(await app.Clients.ListAsync());
    }

    [Fact(DisplayName = "SC-003: Add a person (FR-003)")]
    public async Task SC003_AddAPerson()
    {
        using var app = new ProjectSetupFixture();

        var result = await app.People.CreateAsync("Sam Rivers", "sam@acme.test", PersonRole.Employee);

        Assert.True(result.IsSuccess);
        var people = await app.People.ListAsync();
        Assert.Contains(people, p => p.FullName == "Sam Rivers");

        // "And they are selectable when assigning people to a project".
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var detail = await app.Projects.GetDetailAsync(project.Id);
        Assert.Contains(detail!.AssignablePeople, p => p.Id == result.Value.Id);
    }

    [Fact(DisplayName = "SC-004: Duplicate email is rejected (FR-004)")]
    public async Task SC004_DuplicateEmailIsRejected()
    {
        using var app = new ProjectSetupFixture();
        await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");

        var result = await app.People.CreateAsync("Sam R.", "SAM@ACME.TEST", PersonRole.Employee);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("already in use"));
        Assert.Single(await app.People.ListAsync());
    }

    [Fact(DisplayName = "SC-005: Create a project with a budget (FR-005)")]
    public async Task SC005_CreateAProjectWithABudget()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");

        var result = await app.Projects.CreateAsync("Website Redesign", client.Id, "120");

        Assert.True(result.IsSuccess);
        Assert.Equal(120 * 60, result.Value.BudgetMinutes);

        var row = await app.ProjectRowAsync("Website Redesign");
        Assert.Equal(0, row.Budget.BurnedMinutes);
        Assert.Equal(120 * 60, row.Budget.RemainingMinutes);
    }

    [Fact(DisplayName = "SC-006: A project cannot exist without a client (FR-006)")]
    public async Task SC006_AProjectCannotExistWithoutAClient()
    {
        using var app = new ProjectSetupFixture();

        var result = await app.Projects.CreateAsync("Website Redesign", Guid.Empty, "120");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message == "A client is required.");
        Assert.Empty(await app.Projects.ListAsync());
    }

    [Fact(DisplayName = "SC-007: Budget must be greater than zero (FR-005)")]
    public async Task SC007_BudgetMustBeGreaterThanZero()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");

        var result = await app.Projects.CreateAsync("Website Redesign", client.Id, "0");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("greater than zero"));
        Assert.Empty(await app.Projects.ListAsync());
    }

    [Fact(DisplayName = "SC-008: The same project name under two clients is allowed (FR-007)")]
    public async Task SC008_SameProjectNameUnderTwoClientsIsAllowed()
    {
        using var app = new ProjectSetupFixture();
        var acme = await app.GivenClientAsync("Acme Corp");
        var globex = await app.GivenClientAsync("Globex");
        await app.GivenProjectAsync("Website", acme.Id, "100");

        var result = await app.Projects.CreateAsync("Website", globex.Id, "80");

        Assert.True(result.IsSuccess);
        var projects = await app.Projects.ListAsync();
        Assert.Equal(2, projects.Count(p => p.Name == "Website"));
    }

    [Fact(DisplayName = "SC-009: Duplicate project name for one client is rejected (FR-007)")]
    public async Task SC009_DuplicateProjectNameForOneClientIsRejected()
    {
        using var app = new ProjectSetupFixture();
        var acme = await app.GivenClientAsync("Acme Corp");
        await app.GivenProjectAsync("Website", acme.Id, "100");

        var result = await app.Projects.CreateAsync("website", acme.Id, "80");

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("already has a project"));
        Assert.Single(await app.Projects.ListAsync());
    }

    [Fact(DisplayName = "SC-010: Edit a project's budget (FR-008)")]
    public async Task SC010_EditAProjectsBudget()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");

        var result = await app.Projects.UpdateAsync(project.Id, "Website Redesign", client.Id, "160");

        Assert.True(result.IsSuccess);
        var row = await app.ProjectRowAsync("Website Redesign");
        Assert.Equal(160 * 60, row.Budget.BudgetMinutes);
        Assert.Equal(160 * 60, row.Budget.RemainingMinutes);
    }

    [Fact(DisplayName = "SC-011: Cutting the budget below hours already burned (FR-008, FR-015)")]
    public async Task SC011_CuttingTheBudgetBelowHoursAlreadyBurned()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, minutes: 60 * 60);

        var result = await app.Projects.UpdateAsync(project.Id, "Website Redesign", client.Id, "40");

        Assert.True(result.IsSuccess);
        var row = await app.ProjectRowAsync("Website Redesign");
        Assert.Equal(-20 * 60, row.Budget.RemainingMinutes);
        Assert.Equal(BudgetStatus.OverBudget, row.Budget.Status);
    }

    [Fact(DisplayName = "SC-012: Assign people to a project (FR-009, FR-012)")]
    public async Task SC012_AssignPeopleToAProject()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        var kit = await app.GivenPersonAsync("Kit Lowe", "kit@acme.test");

        var result = await app.Assignments.AssignAsync(project.Id, [sam.Id, kit.Id]);

        Assert.True(result.IsSuccess);
        var detail = await app.Projects.GetDetailAsync(project.Id);
        Assert.Equal(["Kit Lowe", "Sam Rivers"], detail!.AssignedPeople.Select(p => p.FullName));
    }

    [Fact(DisplayName = "SC-013: A person cannot be assigned twice (FR-010)")]
    public async Task SC013_APersonCannotBeAssignedTwice()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.Assignments.AssignAsync(project.Id, [sam.Id]);

        var result = await app.Assignments.AssignAsync(project.Id, [sam.Id]);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, e => e.Message.Contains("already assigned"));

        var detail = await app.Projects.GetDetailAsync(project.Id);
        Assert.Single(detail!.AssignedPeople, p => p.PersonId == sam.Id);
    }

    [Fact(DisplayName = "SC-014: Unassigning a person who has logged hours (FR-011)")]
    public async Task SC014_UnassigningAPersonWhoHasLoggedHours()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.Assignments.AssignAsync(project.Id, [sam.Id]);
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, minutes: 12 * 60);

        // "Then I am warned that 12 logged hours will remain on the project"
        var warned = await app.Assignments.UnassignAsync(project.Id, sam.Id, confirmed: false);
        Assert.Equal(UnassignOutcome.NeedsConfirmation, warned.Outcome);
        Assert.Equal(12 * 60, warned.LoggedMinutes);

        // "When I confirm"
        var confirmed = await app.Assignments.UnassignAsync(project.Id, sam.Id, confirmed: true);
        Assert.Equal(UnassignOutcome.Removed, confirmed.Outcome);

        var detail = await app.Projects.GetDetailAsync(project.Id);
        Assert.DoesNotContain(detail!.AssignedPeople, p => p.PersonId == sam.Id);

        // "And the project still counts those 12 hours as burned"
        Assert.Equal(12 * 60, detail.Budget.BurnedMinutes);

        // "And Sam Rivers can no longer log new hours against it" — there is no assignment left for
        // stories 005–009 to gate on, which is the whole of what this slice can assert.
        Assert.False(await app.Db.Assignments.AnyAsync(a => a.ProjectId == project.Id && a.PersonId == sam.Id));
    }

    [Fact(DisplayName = "SC-015: The project list shows budget, burned and remaining (FR-013)")]
    public async Task SC015_TheProjectListShowsBudgetBurnedAndRemaining()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, minutes: 45 * 60);

        var row = await app.ProjectRowAsync("Website Redesign");

        Assert.Equal("Acme Corp", row.ClientName);
        Assert.Equal(120 * 60, row.Budget.BudgetMinutes);
        Assert.Equal(45 * 60, row.Budget.BurnedMinutes);
        Assert.Equal(75 * 60, row.Budget.RemainingMinutes);
    }

    [Fact(DisplayName = "SC-016: A project with no logged hours reads zero, not blank (FR-017)")]
    public async Task SC016_AProjectWithNoLoggedHoursReadsZero()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        await app.GivenProjectAsync("Website Redesign", client.Id, "120");

        var row = await app.ProjectRowAsync("Website Redesign");

        Assert.Equal(0, row.Budget.BurnedMinutes);
        Assert.Equal(120 * 60, row.Budget.RemainingMinutes);
    }

    [Fact(DisplayName = "SC-017: Projects near and over budget are distinguished (FR-016)")]
    public async Task SC017_ProjectsNearAndOverBudgetAreDistinguished()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");

        var alpha = await app.GivenProjectAsync("Alpha", client.Id, "100");
        var beta = await app.GivenProjectAsync("Beta", client.Id, "100");
        var gamma = await app.GivenProjectAsync("Gamma", client.Id, "100");

        await app.GivenLoggedHoursAsync(alpha.Id, sam.Id, 80 * 60);
        await app.GivenLoggedHoursAsync(beta.Id, sam.Id, 95 * 60);
        await app.GivenLoggedHoursAsync(gamma.Id, sam.Id, 110 * 60);

        Assert.Equal(BudgetStatus.WithinBudget, (await app.ProjectRowAsync("Alpha")).Budget.Status);
        Assert.Equal(BudgetStatus.ApproachingBudget, (await app.ProjectRowAsync("Beta")).Budget.Status);
        Assert.Equal(BudgetStatus.OverBudget, (await app.ProjectRowAsync("Gamma")).Budget.Status);
    }

    [Fact(DisplayName = "SC-018: Burned hours include unapproved time (FR-014)")]
    public async Task SC018_BurnedHoursIncludeUnapprovedTime()
    {
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "100");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");

        await app.GivenLoggedHoursAsync(project.Id, sam.Id, 10 * 60, TimesheetWeekStatus.Approved, new DateOnly(2026, 8, 24));
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, 15 * 60, TimesheetWeekStatus.Submitted, new DateOnly(2026, 8, 31));
        await app.GivenLoggedHoursAsync(project.Id, sam.Id, 5 * 60, TimesheetWeekStatus.Draft, new DateOnly(2026, 9, 7));

        var row = await app.ProjectRowAsync("Website Redesign");

        Assert.Equal(30 * 60, row.Budget.BurnedMinutes);
        Assert.Equal(70 * 60, row.Budget.RemainingMinutes);
    }

    [Fact(DisplayName = "SC-019: An employee cannot perform project setup (FR-018)")]
    public async Task SC019_AnEmployeeCannotPerformProjectSetup()
    {
        // Set the world up as a manager, then act as an employee against the same database, so the
        // "no data is changed" half of the scenario is a claim about the very rows they attacked.
        using var app = new ProjectSetupFixture();
        var client = await app.GivenClientAsync("Acme Corp");
        var project = await app.GivenProjectAsync("Website Redesign", client.Id, "120");
        var sam = await app.GivenPersonAsync("Sam Rivers", "sam@acme.test");
        await app.Assignments.AssignAsync(project.Id, [sam.Id]);

        var employee = app.AsEmployee;

        await Assert.ThrowsAsync<ForbiddenException>(() => employee.Clients.CreateAsync("Globex"));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            employee.People.CreateAsync("Kit Lowe", "kit@acme.test", PersonRole.Employee));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            employee.Projects.CreateAsync("Another", client.Id, "50"));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            employee.Projects.UpdateAsync(project.Id, "Renamed", client.Id, "999"));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            employee.Assignments.AssignAsync(project.Id, [sam.Id]));
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            employee.Assignments.UnassignAsync(project.Id, sam.Id, confirmed: true));

        // "And no data is changed."
        Assert.Single(await app.Clients.ListAsync());
        Assert.Single(await app.People.ListAsync());
        var row = await app.ProjectRowAsync("Website Redesign");
        Assert.Equal("Website Redesign", row.Name);
        Assert.Equal(120 * 60, row.Budget.BudgetMinutes);
        Assert.Equal(1, row.AssignedPeopleCount);
    }
}
