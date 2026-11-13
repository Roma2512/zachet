using System.Text.Json;
using Variant05.Models;

namespace Variant05.Services;

// TODO 9: Добавить JSON-журнал.
public sealed class JsonLogger : IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly List<BackupLogEntry> _entries = new();
    private readonly string _path;

    public JsonLogger(string path) => _path = path;

    public async Task AddAsync(BackupLogEntry entry, CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            _entries.Add(entry);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(CancellationToken ct = default)
    {
        await _lock.WaitAsync(ct);
        try
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(_entries, options);
            await File.WriteAllTextAsync(_path, json, ct);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose() => _lock.Dispose();
}