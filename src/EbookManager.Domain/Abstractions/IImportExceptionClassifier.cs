namespace EbookManager.Domain.Abstractions;

public interface IImportExceptionClassifier
{
    bool IsDuplicateKeyViolation(Exception exception);
}
