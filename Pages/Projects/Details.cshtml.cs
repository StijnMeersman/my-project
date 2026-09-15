using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;
using my_project.Web;

namespace my_project.Pages.Projects;

/// <summary>
/// One project: its budget position, and who may log hours against it (spec 001 US-004, FR-009–FR-013).
/// <para>
/// The page itself is readable by anyone — FR-018 restricts changing assignments, not seeing them —
/// so the guard sits on the two write handlers rather than on the class.
/// </para>
/// </summary>
public class DetailsModel(ProjectService projects, AssignmentService assignments, ICurrentUser currentUser)
    : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    public ProjectDetail Project { get; private set; } = null!;

    public string? Notice { get; private set; }

    /// <summary>Set when unassigning someone would strand logged hours, so FR-011 can ask first.</summary>
    public PendingUnassign? AwaitingConfirmation { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct) => await ShowAsync(ct);

    public async Task<IActionResult> OnPostAssignAsync(Guid[] personIds, CancellationToken ct)
    {
        if (!currentUser.IsManager)
        {
            return Forbid403();
        }

        var result = await assignments.AssignAsync(Id, personIds, ct);
        if (!result.IsSuccess)
        {
            result.CopyErrorsTo(ModelState, prefix: string.Empty);
            return await ShowAsync(ct);
        }

        return RedirectToPage(new { id = Id });
    }

    public async Task<IActionResult> OnPostUnassignAsync(Guid personId, bool confirmed, CancellationToken ct)
    {
        if (!currentUser.IsManager)
        {
            return Forbid403();
        }

        var result = await assignments.UnassignAsync(Id, personId, confirmed, ct);

        if (result.Outcome == UnassignOutcome.NeedsConfirmation)
        {
            var page = await ShowAsync(ct);
            var person = Project?.AssignedPeople.FirstOrDefault(p => p.PersonId == personId);
            if (person is not null)
            {
                AwaitingConfirmation = new PendingUnassign(personId, person.FullName, result.LoggedMinutes);
            }

            return page;
        }

        return RedirectToPage(new { id = Id });
    }

    private async Task<IActionResult> ShowAsync(CancellationToken ct)
    {
        var detail = await projects.GetDetailAsync(Id, ct);
        if (detail is null)
        {
            return NotFound();
        }

        Project = detail;
        return Page();
    }

    private static IActionResult Forbid403() => new StatusCodeResult(StatusCodes.Status403Forbidden);

    public sealed record PendingUnassign(Guid PersonId, string FullName, int LoggedMinutes);
}
