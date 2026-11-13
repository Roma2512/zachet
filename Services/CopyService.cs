using System.Diagnostics;
using Variant05.Models;

namespace Variant05.Services;

public sealed class CopyService
{
    private readonly HashService _hash = new();

    // TODO 2: Реализовать последовательное копирование.
    public async Task CopySequentialAsync(
        IReadOnlyList<BackupFile> files,
        string destinationDirectory,
        CancellationToken ct = default)
    {
        foreach (var file in files)
        {
            var result = await CopyAndVerifyAsync(file, destinationDirectory, ct);
            PrintResult("Seq", result);
        }
    }

    // TODO 3: Реализовать асинхронное копирование потоками.
    // TODO 4: Ограничить параллельные копирования через SemaphoreSlim.
    public async Task CopyConcurrentAsync(
        IReadOnlyList<BackupFile> files,
        string destinationDirectory,
        int maxParallel,
        CancellationToken ct = default)
    {
        using var semaphore = new SemaphoreSlim(maxParallel, maxParallel);
        var tasks = new List<Task>();

        foreach (var file in files)
        {
            await semaphore.WaitAsync(ct);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await CopyAndVerifyAsync(file, destinationDirectory, ct);
                    PrintResult("Par", result);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
    }

    // TODO 5: Проверять целостность копии через SHA256.
    // TODO 6: Обрабатывать ошибки каждого файла и закрывать потоки.
    public async Task<CopyResult> CopyAndVerifyAsync(
        BackupFile file,
        string destinationDirectory,
        CancellationToken ct = default)
    {
        var destinationPath = Path.Combine(destinationDirectory, file.RelativePath);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var destDir = Path.GetDirectoryName(destinationPath);
            if (!string.IsNullOrEmpty(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            var sourceHash = await _hash.ComputeSha256Async(file.SourcePath, ct);

            await using (var src = new FileStream(file.SourcePath, FileMode.Open, FileAccess.Read,
                                                  FileShare.Read, 81920, useAsync: true))
            await using (var dst = new FileStream(destinationPath, FileMode.Create, FileAccess.Write,
                                                  FileShare.None, 81920, useAsync: true))
            {
                await src.CopyToAsync(dst, ct);
            }

            var destHash = await _hash.ComputeSha256Async(destinationPath, ct);
            var verified = string.Equals(sourceHash, destHash, StringComparison.OrdinalIgnoreCase);

            stopwatch.Stop();

            if (!verified)
            {
                return new CopyResult
                {
                    File = file,
                    DestinationPath = destinationPath,
                    Success = false,
                    HashVerified = false,
                    ElapsedMs = stopwatch.ElapsedMilliseconds,
                    Error = "SHA256 источника и копии не совпадают"
                };
            }

            return new CopyResult
            {
                File = file,
                DestinationPath = destinationPath,
                Success = true,
                HashVerified = true,
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return new CopyResult
            {
                File = file,
                DestinationPath = destinationPath,
                Success = false,
                HashVerified = false,
                ElapsedMs = stopwatch.ElapsedMilliseconds,
                Error = ex.Message
            };
        }
    }

    private static void PrintResult(string tag, CopyResult r)
    {
        if (r.Success)
            Console.WriteLine($"[{tag}] OK   {r.File.RelativePath} ({r.ElapsedMs} ms)");
        else
            Console.WriteLine($"[{tag}] FAIL {r.File.RelativePath}: {r.Error}");
    }
}