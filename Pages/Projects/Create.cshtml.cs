using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using my_project.Application;
using my_project.Web;

namespace my_project.Pages.Projects;

/// <summary>
/// Creating a project (spec 001 US-003, FR-005–FR-007). One screen, three inputs — NFR-006 treats
/// setup friction as the thing most likely to stop the app being used at all.
/// </summary>
[ManagerOnly]
public class CreateModel(ProjectService projects, ClientService clients) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public SelectList ClientOptions { get; private set; } = new(Array.Empty<object>());

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        await LoadClientsAsync(ct);

        return ClientOptions.Count() == 0
            ? RedirectToPage("/Clients/Index")   // A project cannot exist without a client (FR-006).
            : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await projects.CreateAsync(Input.Name, Input.ClientId, Input.BudgetMinutes, ct);

        if (!result.IsSuccess)
        {
            result.CopyErrorsTo(ModelState, nameof(Input));
            await LoadClientsAsync(ct);
            return Page();
        }

        return RedirectToPage("Details", new { id = result.Value.Id });
    }

    private async Task LoadClientsAsync(CancellationToken ct) =>
        ClientOptions = new SelectList(await clients.ListAsync(ct), "Id", "Name", Input.ClientId);

    public class InputModel
    {
        [Display(Name = "Project name")]
        public string? Name { get; set; }

        [Display(Name = "Client")]
        public Guid ClientId { get; set; }

        /// <summary>
        /// Bound as text, not as a number: it accepts both <c>120</c> and <c>120:30</c>, and a
        /// rejected value has to come back to the user exactly as they typed it (EC-1, EC-3).
        /// </summary>
        [Display(Name = "Budget")]
        public string? BudgetMinutes { get; set; }
    }
}
