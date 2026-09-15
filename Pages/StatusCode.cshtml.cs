using Microsoft.AspNetCore.Mvc.RazorPages;

namespace my_project.Pages;

public class StatusCodeModel : PageModel
{
    public int Code { get; private set; }

    public string Heading { get; private set; } = "Something went wrong";

    public string Explanation { get; private set; } = "Try again, or go back to the projects list.";

    public void OnGet(int? code)
    {
        Code = code ?? 500;

        if (Code == StatusCodes.Status403Forbidden)
        {
            Heading = "That is a manager's job";
            Explanation = "Creating clients, people and projects, and changing who is assigned, is limited to managers.";
        }
        else if (Code == StatusCodes.Status404NotFound)
        {
            Heading = "Nothing here";
            Explanation = "That page or record does not exist.";
        }
    }
}
