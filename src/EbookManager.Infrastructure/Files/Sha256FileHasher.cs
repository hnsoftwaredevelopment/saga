using System.Buffers;
using System.Security.Cryptography;
using EbookManager.Domain.Abstractions;

namespace EbookManager.Infrastructure.Files;

public sealed class Sha256FileHasher : IFileHasher
{
    private const int BufferSize = 1024 * 1024;

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
                BufferSize = BufferSize,
                Options = FileOptions.Asynchronous | FileOptions.SequentialScan
            });

        using var incrementalHash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = ArrayPool<byte>.Shared.Rent(BufferSize);
        try
        {
            while (true)
            {
                var bytesRead = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (bytesRead == 0)
                {
                    break;
                }

                incrementalHash.AppendData(buffer.AsSpan(0, bytesRead));
            }

            return Convert.ToHexString(incrementalHash.GetHashAndReset());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
