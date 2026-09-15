using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;

namespace my_project.Pages.Projects;

/// <summary>
/// Every project with its budget, hours burned and hours remaining (spec 001 US-005, FR-013–FR-017).
/// <para>
/// Deliberately not <c>[ManagerOnly]</c>: FR-018 restricts creating and editing, not looking. The
/// buttons are hidden for an employee and the services refuse them regardless.
/// </para>
/// </summary>
public class IndexModel(ProjectService projects) : PageModel
{
    public IReadOnlyList<ProjectListItem> Projects { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken ct) => Projects = await projects.ListAsync(ct);
}
