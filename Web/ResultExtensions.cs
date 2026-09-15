using Microsoft.AspNetCore.Mvc.ModelBinding;
using my_project.Common;

namespace my_project.Web;

public static class ResultExtensions
{
    /// <summary>
    /// Moves a domain result's errors onto the form fields that caused them, so each message lands
    /// next to its input and whatever was typed is kept (spec 001 EC-1).
    /// </summary>
    public static void CopyErrorsTo(this Result result, ModelStateDictionary modelState, string prefix)
    {
        foreach (var error in result.Errors)
        {
            var key = error.Field.Length == 0 ? string.Empty : $"{prefix}.{error.Field}";
            modelState.AddModelError(key, error.Message);
        }
    }
}
