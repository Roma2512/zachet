using Variant05.Models;

namespace Variant05.Services;

public sealed class FileDiscoveryService
{
    public List<BackupFile> Discover(string sourceDirectory)
    {
        var result = new List<BackupFile>();
        var sourceFull = Path.GetFullPath(sourceDirectory);

        foreach (var path in Directory.EnumerateFiles(sourceFull, "*", SearchOption.AllDirectories))
        {
            var info = new FileInfo(path);
            result.Add(new BackupFile
            {
                SourcePath = path,
                RelativePath = Path.GetRelativePath(sourceFull, path),
                Length = info.Length
            });
        }

        return result;
    }
}