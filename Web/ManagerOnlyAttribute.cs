using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using my_project.Application;

namespace my_project.Web;

/// <summary>
/// Refuses a page to anyone who is not a manager (spec 001 FR-018).
/// <para>
/// This is the outer of two gates. The services enforce the same rule themselves, because a filter
/// only covers the requests that happen to pass through it and NFR-002 asks for enforcement on
/// every action, not on every page.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public sealed class ManagerOnlyAttribute : Attribute, IAsyncPageFilter
{
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        var currentUser = context.HttpContext.RequestServices.GetRequiredService<ICurrentUser>();

        if (!currentUser.IsManager)
        {
            // A plain 403 rather than ForbidResult: there is no authentication scheme registered to
            // hand a challenge to yet (spec 001 §10 Q1). UseStatusCodePagesWithReExecute turns this
            // into the explanation page.
            context.Result = new StatusCodeResult(StatusCodes.Status403Forbidden);
            return;
        }

        await next();
    }
}
