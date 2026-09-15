using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace my_project.Application;

/// <summary>
/// Recognises the database refusing a duplicate.
/// <para>
/// Every uniqueness rule in this slice is checked twice: once with a query, to produce a friendly
/// message, and once by a unique index, which is the only check that survives two managers writing
/// at the same moment (spec 001 EC-9). This turns the second one back into the same friendly
/// message rather than a 500.
/// </para>
/// </summary>
public static class UniqueIndexViolation
{
    private const int SqliteConstraint = 19;

    public static bool Caused(DbUpdateException exception) =>
        exception.InnerException is SqliteException { SqliteErrorCode: SqliteConstraint };
}
