namespace EbookManager.Application.Importing;

public sealed class ImportPersistenceException : InvalidOperationException
{
    public ImportPersistenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
