namespace EbookManager.Domain.Libraries;

public sealed record LibraryDescriptor(
    string Name,
    string DirectoryPath,
    DateTimeOffset LastOpenedUtc);
