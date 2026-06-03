namespace EbookManager.Infrastructure.Settings;

public static class SyncfusionLicenseKeyResolver
{
    private static readonly string[] LicenseFileNames =
    [
        Path.Combine("docs", "SynfusionLicense.txt"),
        Path.Combine("docs", "SyncfusionLicense.txt")
    ];

    public static string? Resolve(string? environmentKey, IEnumerable<string?> fileKeys)
    {
        if (!string.IsNullOrWhiteSpace(environmentKey))
        {
            return environmentKey.Trim();
        }

        return fileKeys
            .FirstOrDefault(key => !string.IsNullOrWhiteSpace(key))
            ?.Trim();
    }

    public static string? ResolveFromEnvironmentAndLocalFiles()
    {
        var fileKeys = EnumerateCandidateLicenseFiles()
            .Select(ReadLicenseFile);
        return Resolve(Environment.GetEnvironmentVariable("SYNCFUSION_LICENSE_KEY"), fileKeys);
    }

    private static IEnumerable<string> EnumerateCandidateLicenseFiles()
    {
        var roots = new[]
        {
            AppContext.BaseDirectory,
            Directory.GetCurrentDirectory()
        };

        return roots
            .Where(root => !string.IsNullOrWhiteSpace(root))
            .SelectMany(EnumerateCandidateLicenseFilesFromRoot)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> EnumerateCandidateLicenseFilesFromRoot(string root)
    {
        var directory = new DirectoryInfo(root);
        while (directory is not null)
        {
            foreach (var fileName in LicenseFileNames)
            {
                yield return Path.Combine(directory.FullName, fileName);
            }

            directory = directory.Parent;
        }
    }

    private static string? ReadLicenseFile(string path)
    {
        try
        {
            return File.Exists(path) ? File.ReadAllText(path) : null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }
}
