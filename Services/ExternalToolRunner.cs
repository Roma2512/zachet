using System.Diagnostics;

namespace Variant05.Services;

// TODO 10: Добавить запуск внешней утилиты и ожидание её завершения через Process.
public sealed class ExternalToolRunner
{
    public async Task<int> RunAsync(string fileName, string arguments, CancellationToken ct = default)
    {
        var psi = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null) Console.WriteLine($"[Tool] {e.Data}");
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) Console.WriteLine($"[Tool:err] {e.Data}");
        };

        if (!process.Start())
            throw new InvalidOperationException($"Не удалось запустить '{fileName}'");

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        await process.WaitForExitAsync(ct);
        return process.ExitCode;
    }
}