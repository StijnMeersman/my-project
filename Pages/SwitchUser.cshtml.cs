using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;

namespace my_project.Pages;

/// <summary>
/// The dev-only person switcher behind <see cref="DevUserSwitchCurrentUser"/>. It exists so the
/// Manager-only rule (spec 001 FR-018, SC-019) and the own-week rule (spec 005 FR-022, SC-024) can
/// be seen working before authentication exists. Delete this page when the auth story lands.
/// </summary>
public class SwitchUserModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost(Guid personId, string? returnUrl)
    {
        Response.Cookies.Append(
            DevUserSwitchCurrentUser.CookieName,
            personId.ToString(),
            new CookieOptions { HttpOnly = true, IsEssential = true, SameSite = SameSiteMode.Lax });

        // Only ever bounce back into this app, never to a caller-supplied absolute URL.
        return Url.IsLocalUrl(returnUrl) ? LocalRedirect(returnUrl) : RedirectToPage("/Index");
    }
}
