namespace Variant05.Models;

public sealed class BackupLogEntry
{
    public DateTime TimestampUtc { get; set; }
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public long Size { get; set; }
    public string SourceHash { get; set; } = "";
    public string DestinationHash { get; set; } = "";
    public bool Success { get; set; }
    public string? Error { get; set; }
}