using System.Threading.Channels;
using Variant05.Models;

namespace Variant05.Services;

// TODO 8: Построить pipeline обнаружение → копирование → проверка через Channel.
public sealed class BackupPipeline
{
    private readonly CopyService _copyService;
    private readonly HashService _hashService;
    private readonly JsonLogger _logger;

    public BackupPipeline(CopyService copyService, HashService hashService, JsonLogger logger)
    {
        _copyService = copyService;
        _hashService = hashService;
        _logger = logger;
    }

    public async Task RunAsync(
        string sourceDirectory,
        string destinationDirectory,
        int maxParallel,
        CancellationToken ct = default)
    {
        var fileChannel = Channel.CreateBounded<BackupFile>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = true,
            SingleReader = false
        });

        var resultChannel = Channel.CreateBounded<CopyResult>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = true
        });

        var producerTask = Task.Run(async () =>
        {
            try
            {
                var discovery = new FileDiscoveryService();
                foreach (var file in discovery.Discover(sourceDirectory))
                {
                    await fileChannel.Writer.WriteAsync(file, ct);
                }
            }
            finally
            {
                fileChannel.Writer.Complete();
            }
        }, ct);

        var copierTask = Task.Run(async () =>
        {
            using var semaphore = new SemaphoreSlim(maxParallel, maxParallel);
            var tasks = new List<Task>();

            try
            {
                await foreach (var file in fileChannel.Reader.ReadAllAsync(ct))
                {
                    await semaphore.WaitAsync(ct);

                    tasks.Add(Task.Run(async () =>
                    {
                        try
                        {
                            var result = await _copyService.CopyAndVerifyAsync(file, destinationDirectory, ct);
                            await resultChannel.Writer.WriteAsync(result, ct);
                        }
                        catch (Exception ex)
                        {
                            await resultChannel.Writer.WriteAsync(new CopyResult
                            {
                                File = file,
                                Success = false,
                                Error = ex.Message
                            }, ct);
                        }
                        finally
                        {
                            semaphore.Release();
                        }
                    }, ct));
                }

                await Task.WhenAll(tasks);
            }
            finally
            {
                resultChannel.Writer.Complete();
            }
        }, ct);

        var verifierTask = Task.Run(async () =>
        {
            await foreach (var result in resultChannel.Reader.ReadAllAsync(ct))
            {
                var entry = new BackupLogEntry
                {
                    TimestampUtc = DateTime.UtcNow,
                    Source = result.File.SourcePath,
                    Destination = result.DestinationPath,
                    Size = result.File.Length,
                    Success = result.Success,
                    Error = result.Error
                };

                if (result.Success)
                {
                    try
                    {
                        entry.SourceHash = await _hashService.ComputeSha256Async(result.File.SourcePath, ct);
                        entry.DestinationHash = await _hashService.ComputeSha256Async(result.DestinationPath, ct);

                        if (!string.Equals(entry.SourceHash, entry.DestinationHash, StringComparison.OrdinalIgnoreCase))
                        {
                            entry.Success = false;
                            entry.Error = "Хеши не совпадают при контрольной проверке";
                        }
                    }
                    catch (Exception ex)
                    {
                        entry.Success = false;
                        entry.Error = $"Ошибка при проверке хеша: {ex.Message}";
                    }
                }

                await _logger.AddAsync(entry, ct);

                if (entry.Success)
                    Console.WriteLine($"[Pipe] OK   {result.File.RelativePath}");
                else
                    Console.WriteLine($"[Pipe] FAIL {result.File.RelativePath}: {entry.Error}");
            }
        }, ct);

        await Task.WhenAll(producerTask, copierTask, verifierTask);
    }
}