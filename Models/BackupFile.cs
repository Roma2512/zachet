namespace Variant05.Models;

public sealed class BackupFile
{
    public required string SourcePath { get; init; }
    public required string RelativePath { get; init; }
    public required long Length { get; init; }
}