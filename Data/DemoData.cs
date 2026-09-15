using Microsoft.EntityFrameworkCore;
using my_project.Domain;

namespace my_project.Data;

/// <summary>
/// Development-only seed data, written only into an empty database.
/// <para>
/// Its job is to make the parts of spec 001 that depend on logged hours visible without waiting for
/// story 006: a project comfortably within budget, one approaching it and one over it (FR-016,
/// SC-017), with hours spread across draft, submitted and approved weeks to show that all of them
/// count (FR-014, SC-018).
/// </para>
/// </summary>
public static class DemoData
{
    public static async Task SeedAsync(TimeRegistrationDbContext db, TimeProvider clock)
    {
        if (await db.Clients.AnyAsync())
        {
            return;
        }

        var now = clock.GetUtcNow();

        var acme = Client.Create("Acme Corp", now).Value;
        var globex = Client.Create("Globex", now).Value;
        db.Clients.AddRange(acme, globex);

        var sam = Person.Create("Sam Rivers", "sam@acme.test", PersonRole.Employee, now).Value;
        var kit = Person.Create("Kit Lowe", "kit@acme.test", PersonRole.Employee, now).Value;
        var robin = Person.Create("Robin Vos", "robin@acme.test", PersonRole.Manager, now).Value;
        db.People.AddRange(sam, kit, robin);

        var redesign = Project.Create("Website Redesign", acme.Id, "120", now).Value;   // well within budget
        var retainer = Project.Create("Support Retainer", acme.Id, "40", now).Value;    // over budget
        var portal = Project.Create("Customer Portal", globex.Id, "100", now).Value;    // approaching budget
        var greenfield = Project.Create("Brand Refresh", globex.Id, "60", now).Value;   // nothing logged yet
        db.Projects.AddRange(redesign, retainer, portal, greenfield);

        db.Assignments.AddRange(
            Assignment.Create(redesign.Id, sam.Id, now),
            Assignment.Create(redesign.Id, kit.Id, now),
            Assignment.Create(retainer.Id, sam.Id, now),
            Assignment.Create(portal.Id, kit.Id, now),
            Assignment.Create(portal.Id, robin.Id, now));

        // Three consecutive past weeks, each in a different state, so the project list can be
        // checked against the claim that burned hours ignores status entirely.
        var thisMonday = MondayOf(DateOnly.FromDateTime(now.UtcDateTime));
        var weeks = new[]
        {
            (Start: thisMonday.AddDays(-21), Status: TimesheetWeekStatus.Approved),
            (Start: thisMonday.AddDays(-14), Status: TimesheetWeekStatus.Submitted),
            (Start: thisMonday.AddDays(-7), Status: TimesheetWeekStatus.Draft),
        };

        foreach (var (start, status) in weeks)
        {
            foreach (var person in new[] { sam, kit, robin })
            {
                db.TimesheetWeeks.Add(TimesheetWeek.Create(person.Id, start, status, now));
            }
        }

        // 45:00 burned of 120:00 — within budget.
        AddWeekdayEntries(db, sam.Id, redesign.Id, weeks[0].Start, minutesPerDay: 180, days: 5, now);   // 15:00
        AddWeekdayEntries(db, kit.Id, redesign.Id, weeks[1].Start, minutesPerDay: 240, days: 5, now);   // 20:00
        AddWeekdayEntries(db, sam.Id, redesign.Id, weeks[2].Start, minutesPerDay: 120, days: 5, now);   // 10:00

        // 48:00 burned of 40:00 — over budget, remaining shows -8:00.
        AddWeekdayEntries(db, sam.Id, retainer.Id, weeks[0].Start, minutesPerDay: 240, days: 4, now);   // 16:00
        AddWeekdayEntries(db, sam.Id, retainer.Id, weeks[1].Start, minutesPerDay: 240, days: 4, now);   // 16:00
        AddWeekdayEntries(db, sam.Id, retainer.Id, weeks[2].Start, minutesPerDay: 240, days: 4, now);   // 16:00

        // 93:00 burned of 100:00 — 93%, so flagged as approaching its budget.
        AddWeekdayEntries(db, kit.Id, portal.Id, weeks[0].Start, minutesPerDay: 360, days: 5, now);     // 30:00
        AddWeekdayEntries(db, kit.Id, portal.Id, weeks[1].Start, minutesPerDay: 360, days: 5, now);     // 30:00
        AddWeekdayEntries(db, kit.Id, portal.Id, weeks[2].Start, minutesPerDay: 360, days: 5, now);     // 30:00
        AddWeekdayEntries(db, robin.Id, portal.Id, weeks[2].Start, minutesPerDay: 180, days: 1, now);   //  3:00

        await db.SaveChangesAsync();
    }

    private static void AddWeekdayEntries(
        TimeRegistrationDbContext db,
        Guid personId,
        Guid projectId,
        DateOnly weekStart,
        int minutesPerDay,
        int days,
        DateTimeOffset now)
    {
        for (var day = 0; day < days; day++)
        {
            db.TimeEntries.Add(TimeEntry.Create(
                personId,
                projectId,
                weekStart.AddDays(day),
                Duration.FromMinutes(minutesPerDay),
                note: null,
                now));
        }
    }

    private static DateOnly MondayOf(DateOnly date) =>
        date.AddDays(-((int)date.DayOfWeek + 6) % 7);
}
