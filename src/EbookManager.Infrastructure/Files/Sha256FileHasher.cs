using System.Security.Cryptography;
using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class Sha256FileHasher : IFileHasher
{
    public async Task<string> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await using var stream = new FileStream(
            Path.GetFullPath(path),
            new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.ReadWrite | FileShare.Delete,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash);
    }
}
