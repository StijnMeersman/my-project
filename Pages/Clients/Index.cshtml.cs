using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;
using my_project.Domain;
using my_project.Web;

namespace my_project.Pages.Clients;

/// <summary>Registering clients (spec 001 US-001, FR-001, FR-002).</summary>
[ManagerOnly]
public class IndexModel(ClientService clients) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Client> Clients { get; private set; } = [];

    public string? Confirmation { get; private set; }

    public async Task OnGetAsync(string? created, CancellationToken ct)
    {
        Confirmation = created is null ? null : $"\"{created}\" was registered.";
        Clients = await clients.ListAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await clients.CreateAsync(Input.Name, ct);

        if (!result.IsSuccess)
        {
            result.CopyErrorsTo(ModelState, nameof(Input));
            Clients = await clients.ListAsync(ct);
            return Page();
        }

        // Redirect after post so a refresh does not try to register the client a second time.
        return RedirectToPage(new { created = result.Value.Name });
    }

    public class InputModel
    {
        /// <summary>Validated by <see cref="Client.Create"/>; the attribute only saves a round trip.</summary>
        [Display(Name = "Client name")]
        public string? Name { get; set; }
    }
}
