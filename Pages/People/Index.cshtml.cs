using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;
using my_project.Domain;
using my_project.Web;

namespace my_project.Pages.People;

/// <summary>Adding people to the organisation (spec 001 US-002, FR-003, FR-004).</summary>
[ManagerOnly]
public class IndexModel(PersonService people) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();

    public IReadOnlyList<Person> People { get; private set; } = [];

    public string? Confirmation { get; private set; }

    public async Task OnGetAsync(string? created, CancellationToken ct)
    {
        Confirmation = created is null ? null : $"{created} was added.";
        People = await people.ListAsync(ct);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var result = await people.CreateAsync(Input.FullName, Input.Email, Input.Role, ct);

        if (!result.IsSuccess)
        {
            result.CopyErrorsTo(ModelState, nameof(Input));
            People = await people.ListAsync(ct);
            return Page();
        }

        return RedirectToPage(new { created = result.Value.FullName });
    }

    public class InputModel
    {
        [Display(Name = "Full name")]
        public string? FullName { get; set; }

        [Display(Name = "Email address")]
        public string? Email { get; set; }

        [Display(Name = "Role")]
        public PersonRole Role { get; set; } = PersonRole.Employee;
    }
}
