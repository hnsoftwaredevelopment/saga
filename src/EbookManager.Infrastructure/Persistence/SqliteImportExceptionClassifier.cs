using EbookManager.Domain.Abstractions;
using Microsoft.Data.Sqlite;

namespace EbookManager.Infrastructure.Persistence;

public sealed class SqliteImportExceptionClassifier : IImportExceptionClassifier
{
    public bool IsDuplicateKeyViolation(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SqliteException sqliteException &&
                sqliteException.SqliteErrorCode == 19 &&
                sqliteException.SqliteExtendedErrorCode == 2067)
            {
                return true;
            }
        }

        return false;
    }
}
