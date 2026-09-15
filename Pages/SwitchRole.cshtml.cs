using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;
using my_project.Domain;

namespace my_project.Pages;

/// <summary>
/// The dev-only role switcher behind <see cref="DevRoleSwitchCurrentUser"/>. It exists so the
/// Manager-only rule (spec 001 FR-018, SC-019) can be seen working before authentication exists.
/// Delete this page when the auth story lands.
/// </summary>
public class SwitchRoleModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost(PersonRole role, string? returnUrl)
    {
        Response.Cookies.Append(
            DevRoleSwitchCurrentUser.CookieName,
            role.ToString(),
            new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax });

        // Only ever bounce back into this app, never to a caller-supplied absolute URL.
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Index");
    }
}
