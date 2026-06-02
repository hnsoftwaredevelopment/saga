namespace EbookManager.Domain.Books;

public sealed class BookConflictException : Exception
{
    public BookConflictException()
        : base("Book metadata conflicts with an existing record.")
    {
    }
}
