namespace Variant05.Models;

public sealed class CopyResult
{
    public required BackupFile File { get; init; }
    public string DestinationPath { get; init; } = "";
    public bool Success { get; init; }
    public bool HashVerified { get; init; }
    public long ElapsedMs { get; init; }
    public string? Error { get; init; }
}