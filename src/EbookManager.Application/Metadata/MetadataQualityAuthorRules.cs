namespace EbookManager.Application.Metadata;

public static class MetadataQualityAuthorRules
{
    public static bool IsUsable(string? author) =>
        !string.IsNullOrWhiteSpace(author) &&
        !author.Trim().Equals("Unknown", StringComparison.OrdinalIgnoreCase);
}
