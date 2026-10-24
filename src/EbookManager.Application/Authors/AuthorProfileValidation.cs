namespace EbookManager.Application.Authors;

public static class AuthorProfileValidation
{
    public static string? NormalizeOptional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public static bool IsValidWebsite(string? value) => string.IsNullOrWhiteSpace(value) ||
        (value.Length <= 2048 && Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
         uri.Scheme is "http" or "https" && !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo));

    public static void Validate(string? biography, string? website)
    {
        if (biography?.Length > 10000 || !IsValidWebsite(website))
        {
            throw new ArgumentException("Invalid author profile.");
        }
    }
}
