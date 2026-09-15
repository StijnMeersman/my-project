using Microsoft.EntityFrameworkCore;
using my_project.Domain;

namespace my_project.Data;

public class TimeRegistrationDbContext(DbContextOptions<TimeRegistrationDbContext> options)
    : DbContext(options)
{
    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<Assignment> Assignments => Set<Assignment>();

    public DbSet<TimeEntry> TimeEntries => Set<TimeEntry>();

    public DbSet<TimesheetWeek> TimesheetWeeks => Set<TimesheetWeek>();

    public DbSet<WeekRow> WeekRows => Set<WeekRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Client>(client =>
        {
            client.HasKey(c => c.Id);
            client.Property(c => c.Name).IsRequired().HasMaxLength(TextRules.MaxNameLength);
            client.Property(c => c.NormalizedName).IsRequired().HasMaxLength(TextRules.MaxNameLength);

            // Uniqueness lives on the normalized column rather than on a case-insensitive collation:
            // SQLite's NOCASE only folds ASCII, which would let "İSTANBUL" and "istanbul" coexist (EC-7).
            client.HasIndex(c => c.NormalizedName).IsUnique();
        });

        modelBuilder.Entity<Person>(person =>
        {
            person.HasKey(p => p.Id);
            person.Property(p => p.FullName).IsRequired().HasMaxLength(TextRules.MaxNameLength);
            person.Property(p => p.Email)
                .IsRequired()
                .HasMaxLength(EmailAddress.MaxLength)
                .HasConversion(
                    email => email.Value,
                    value => EmailAddress.Parse(value).Value);
            person.Property(p => p.NormalizedEmail).IsRequired().HasMaxLength(EmailAddress.MaxLength);
            person.Property(p => p.Role).HasConversion<string>().HasMaxLength(20);

            person.HasIndex(p => p.NormalizedEmail).IsUnique();
        });

        modelBuilder.Entity<Project>(project =>
        {
            project.HasKey(p => p.Id);
            project.Property(p => p.Name).IsRequired().HasMaxLength(TextRules.MaxNameLength);
            project.Property(p => p.NormalizedName).IsRequired().HasMaxLength(TextRules.MaxNameLength);

            project.HasOne(p => p.Client)
                .WithMany(c => c.Projects)
                .HasForeignKey(p => p.ClientId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Per client, not globally: two clients may each have a "Website" (FR-007, SC-008).
            project.HasIndex(p => new { p.ClientId, p.NormalizedName }).IsUnique();
        });

        modelBuilder.Entity<Assignment>(assignment =>
        {
            assignment.HasKey(a => a.Id);

            assignment.HasOne(a => a.Project)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.ProjectId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            assignment.HasOne(a => a.Person)
                .WithMany(p => p.Assignments)
                .HasForeignKey(a => a.PersonId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // The database, not the service, is what makes two simultaneous assigns safe (FR-010, EC-9).
            assignment.HasIndex(a => new { a.ProjectId, a.PersonId }).IsUnique();
        });

        modelBuilder.Entity<TimeEntry>(entry =>
        {
            entry.HasKey(e => e.Id);
            entry.Property(e => e.Note).HasMaxLength(500);

            entry.HasOne(e => e.Project)
                .WithMany(p => p.TimeEntries)
                .HasForeignKey(e => e.ProjectId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // No navigation back from Person: removing an assignment must not look like it touches
            // entries, and nothing in this slice reads a person's entries (SC-014, EC-14).
            entry.HasOne(e => e.Person)
                .WithMany()
                .HasForeignKey(e => e.PersonId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            entry.HasIndex(e => new { e.PersonId, e.ProjectId, e.Date }).IsUnique();

            // Burned hours is a per-project aggregate on every project-list render (NFR-001).
            entry.HasIndex(e => e.ProjectId);

            // One person's five days, which is what the weekly grid and every day save read
            // (spec 005 NFR-001, NFR-002). The unique index above leads with PersonId but puts
            // ProjectId before Date, so it cannot serve a date range.
            entry.HasIndex(e => new { e.PersonId, e.Date });
        });

        modelBuilder.Entity<TimesheetWeek>(week =>
        {
            week.HasKey(w => w.Id);
            week.Property(w => w.Status).HasConversion<string>().HasMaxLength(20);

            week.HasOne(w => w.Person)
                .WithMany()
                .HasForeignKey(w => w.PersonId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            week.HasIndex(w => new { w.PersonId, w.WeekStartDate }).IsUnique();
        });

        modelBuilder.Entity<WeekRow>(row =>
        {
            row.HasKey(r => r.Id);

            row.HasOne(r => r.Person)
                .WithMany()
                .HasForeignKey(r => r.PersonId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            row.HasOne(r => r.Project)
                .WithMany()
                .HasForeignKey(r => r.ProjectId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // Two tabs adding the same project to the same week is the case the index, not the
            // service, has to survive (spec 005 FR-006, SC-006). The leading (PersonId,
            // WeekStartDate) pair is also exactly how the grid reads its rows.
            row.HasIndex(r => new { r.PersonId, r.WeekStartDate, r.ProjectId }).IsUnique();
        });
    }
}
