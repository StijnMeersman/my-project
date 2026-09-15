using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using my_project.Application;
using my_project.Common;
using my_project.Domain;

namespace my_project.Pages.Timesheet;

/// <summary>
/// The weekly time grid (spec 005). Rows are projects, columns are the five weekdays, and the unit
/// of commit is a day column — one form, one submit button per day (FR-013, ADR-0005).
/// <para>
/// Not <c>[ManagerOnly]</c> and takes no person: the week is always the signed-in employee's, because
/// <see cref="TimesheetService"/> has no parameter for anyone else's (FR-022, SC-024).
/// </para>
/// </summary>
public class IndexModel(TimesheetService timesheets) : PageModel
{
    private const string LongDayKey = "TimesheetLongDayWarning";
    private const string SavedKey = "TimesheetSavedDay";

    /// <summary>
    /// Any date in the week to show; the service anchors it to its Monday. Named <c>WeekOf</c> rather
    /// than <c>Week</c> so it does not shadow <see cref="Domain.Week"/> inside this class.
    /// </summary>
    [BindProperty(SupportsGet = true, Name = "week")]
    public DateOnly? WeekOf { get; set; }

    /// <summary>Every editable cell of the grid, posted whole and filtered to one day on save.</summary>
    [BindProperty]
    public List<CellInput> Cells { get; set; } = [];

    public TimesheetWeekView? Timesheet { get; private set; }

    /// <summary>
    /// The day column a rejected save was committing, so a cell failure is marked on that one day
    /// rather than on all five cells of its row (EC-8). Null on a plain GET.
    /// </summary>
    public DateOnly? SavedDay { get; private set; }

    /// <summary>FR-015: a long day is worth saying out loud after the save, never worth blocking.</summary>
    public string? Warning { get; private set; }

    public string? Confirmation { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        Warning = TempData[LongDayKey] as string;
        Confirmation = TempData[SavedKey] as string;
        return await ShowAsync(ct);
    }

    /// <summary>FR-005 – FR-007.</summary>
    public async Task<IActionResult> OnPostAddRowAsync(Guid projectId, CancellationToken ct)
    {
        var weekStart = WeekStart();
        var result = await timesheets.AddRowAsync(weekStart, projectId, ct);

        if (!result.IsSuccess)
        {
            Show(result);
            return await ShowAsync(ct);
        }

        return RedirectToWeek(weekStart);
    }

    /// <summary>FR-008.</summary>
    public async Task<IActionResult> OnPostRemoveRowAsync(Guid projectId, CancellationToken ct)
    {
        var weekStart = WeekStart();
        var result = await timesheets.RemoveRowAsync(weekStart, projectId, ct);

        if (!result.IsSuccess)
        {
            Show(result);
            return await ShowAsync(ct);
        }

        return RedirectToWeek(weekStart);
    }

    /// <summary>
    /// FR-013 – FR-016. Only the cells belonging to the day whose button was pressed are sent on:
    /// the form carries the whole week, but a save commits one column of it.
    /// </summary>
    public async Task<IActionResult> OnPostSaveDayAsync(DateOnly day, CancellationToken ct)
    {
        var weekStart = WeekStart();
        SavedDay = day;

        var column = Cells
            .Where(c => c.Date == day)
            .Select(c => new DayCellInput(c.ProjectId, c.Hours, c.Note))
            .ToList();

        var result = await timesheets.SaveDayAsync(weekStart, day, column, ct);

        if (!result.IsSuccess)
        {
            // Redisplay with everything exactly as typed, the offending cells marked (EC-8).
            Show(result);
            return await ShowAsync(ct);
        }

        var saved = result.Value;
        TempData[SavedKey] = $"{Week.NameOfDay(saved.Date)} saved.";

        if (saved.IsLongDay)
        {
            TempData[LongDayKey] =
                $"{Week.NameOfDay(saved.Date)} totals {Duration.ToHhMm(saved.TotalMinutes)}. Saved anyway.";
        }

        // Post/redirect/get, which also re-reads every row's remaining budget (FR-019, NFR-006).
        return RedirectToWeek(weekStart);
    }

    /// <summary>What was typed into a cell, preferred over what is stored when a save was rejected.</summary>
    public CellInput? Posted(Guid projectId, DateOnly date) =>
        Cells.FirstOrDefault(c => c.ProjectId == projectId && c.Date == date);

    public string? HoursError(Guid projectId) => FirstError(TimesheetFields.Hours(projectId));

    public string? NoteError(Guid projectId) => FirstError(TimesheetFields.Note(projectId));

    private string? FirstError(string key) =>
        ModelState.TryGetValue(key, out var entry) && entry.Errors.Count > 0
            ? entry.Errors[0].ErrorMessage
            : null;

    private DateOnly WeekStart() => Week.StartOf(WeekOf ?? timesheets.CurrentWeekStart);

    private async Task<IActionResult> ShowAsync(CancellationToken ct)
    {
        Timesheet = await timesheets.GetWeekAsync(WeekOf, ct);
        return Page();
    }

    /// <summary>
    /// Cell failures keep the service's own key so the grid can mark the input that caused them;
    /// everything else goes to the summary above the grid.
    /// </summary>
    private void Show(Result result)
    {
        foreach (var error in result.Errors)
        {
            var key = error.Field.StartsWith("hours:", StringComparison.Ordinal)
                || error.Field.StartsWith("note:", StringComparison.Ordinal)
                    ? error.Field
                    : string.Empty;

            ModelState.AddModelError(key, error.Message);
        }
    }

    private IActionResult RedirectToWeek(DateOnly weekStart) =>
        RedirectToPage(new { week = weekStart.ToString("yyyy-MM-dd") });

    /// <summary>One posted cell. <see cref="Date"/> is what tells a save which column it belongs to.</summary>
    public sealed class CellInput
    {
        public Guid ProjectId { get; set; }

        public DateOnly Date { get; set; }

        public string? Hours { get; set; }

        public string? Note { get; set; }
    }
}
