using System.Security.Cryptography;

namespace Variant05.Services;

public sealed class HashService
{
    public async Task<string> ComputeSha256Async(string path, CancellationToken ct = default)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);

        var bytes = await SHA256.HashDataAsync(stream, ct);
        return Convert.ToHexString(bytes);
    }
}