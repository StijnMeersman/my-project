using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using my_project.Application;
using my_project.Domain;
using my_project.Web;

namespace my_project.Pages.Projects;

/// <summary>
/// Editing a project's name, client and budget (spec 001 FR-008). Cutting a budget below what has
/// already been burned is allowed — that is the overrun, not a mistake to be blocked (SC-011).
/// </summary>
[ManagerOnly]
public class EditModel(ProjectService projects, ClientService clients) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public Guid Id { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList ClientOptions { get; private set; } = new(Array.Empty<object>());

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var project = await projects.FindAsync(Id, ct);
        if (project is null)
        {
            return NotFound();
        }

        Input = new InputModel
        {
            Name = project.Name,
            ClientId = project.ClientId,
            BudgetMinutes = Duration.ToHhMm(project.BudgetMinutes),
        };

        await LoadClientsAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await projects.UpdateAsync(Id, Input.Name, Input.ClientId, Input.BudgetMinutes, ct);

        if (!result.IsSuccess)
        {
            result.CopyErrorsTo(ModelState, nameof(Input));
            await LoadClientsAsync(ct);
            return Page();
        }

        return RedirectToPage("Details", new { id = Id });
    }

    private async Task LoadClientsAsync(CancellationToken ct) =>
        ClientOptions = new SelectList(await clients.ListAsync(ct), "Id", "Name", Input.ClientId);

    public class InputModel
    {
        [Display(Name = "Project name")]
        public string? Name { get; set; }

        [Display(Name = "Client")]
        public Guid ClientId { get; set; }

        [Display(Name = "Budget")]
        public string? BudgetMinutes { get; set; }
    }
}
