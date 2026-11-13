using System.Diagnostics;
using Variant05.Models;

namespace Variant05.Services;

public sealed class ProjectService
{
    public async Task RunAsync(CancellationToken ct = default)
    {
        // TODO: перенесите предметную логику в отдельные методы и классы.
        // Логика вынесена в FileDiscoveryService, CopyService, HashService,
        // SettingsService, JsonLogger, ExternalToolRunner, BackupPipeline.

        var sourceDirectory = PromptSource();
        if (sourceDirectory is null) return;

        var settings = new SettingsService();

        var destinationDirectory = settings.LoadDestination();
        if (string.IsNullOrWhiteSpace(destinationDirectory))
        {
            destinationDirectory = PromptDestination();
            if (destinationDirectory is null) return;
        }
        else
        {
            Console.WriteLine($"[Settings] Сохранённый ранее каталог: {destinationDirectory}");
            Console.Write("Использовать его? (Y/n): ");
            var answer = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(answer) &&
                answer.Trim().Equals("n", StringComparison.OrdinalIgnoreCase))
            {
                destinationDirectory = PromptDestination();
                if (destinationDirectory is null) return;
            }
        }

        // TODO 7: Сохранять каталог назначения в HKCU.
        settings.SaveDestination(destinationDirectory);
        Directory.CreateDirectory(destinationDirectory);

        // TODO 10: Добавить запуск внешней утилиты и ожидание её завершения через Process.
        var runner = new ExternalToolRunner();
        try
        {
            if (OperatingSystem.IsWindows())
                await runner.RunAsync("cmd.exe", "/c ver", ct);
            else
                await runner.RunAsync("uname", "-a", ct);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Tool] Не удалось запустить утилиту: {ex.Message}");
        }

        // TODO 1: Получить список файлов исходного каталога.
        Console.WriteLine();
        Console.WriteLine("Обнаружение файлов");
        var discovery = new FileDiscoveryService();
        var files = discovery.Discover(sourceDirectory);
        Console.WriteLine($"[Discovery] Найдено файлов: {files.Count}");

        if (files.Count == 0)
        {
            Console.WriteLine("[Discovery] Копировать нечего.");
            return;
        }

        var copyService = new CopyService();

        // TODO 2: Реализовать последовательное копирование.
        Console.WriteLine();
        Console.WriteLine("Последовательное копирование");
        var seqDestination = Path.Combine(destinationDirectory, "sequential");
        Directory.CreateDirectory(seqDestination);

        var sw = Stopwatch.StartNew();
        await copyService.CopySequentialAsync(files, seqDestination, ct);
        sw.Stop();
        Console.WriteLine($"[Sequential] Готово за {sw.ElapsedMilliseconds} ms");

        // TODO 3: Реализовать асинхронное копирование потоками.
        // Реализовано в BackupPipeline (Channel) и CopyService.CopyConcurrentAsync.

        // TODO 4: Ограничить параллельные копирования через SemaphoreSlim.
        // Реализовано в CopyService.CopyConcurrentAsync и BackupPipeline.

        // TODO 5: Проверять целостность копии через SHA256.
        // Реализовано в HashService + CopyService.CopyAndVerifyAsync.

        // TODO 6: Обрабатывать ошибки каждого файла и закрывать потоки.
        // Реализовано в CopyService.CopyAndVerifyAsync (try/catch + await using).

        // TODO 8: Построить pipeline обнаружение → копирование → проверка через Channel.
        // TODO 9: Добавить JSON-журнал.
        var pipelineDestination = Path.Combine(destinationDirectory, "pipeline");
        Directory.CreateDirectory(pipelineDestination);

        var logPath = Path.Combine(destinationDirectory, "backup-log.json");
        using var logger = new JsonLogger(logPath);
        var hashService = new HashService();
        var pipeline = new BackupPipeline(copyService, hashService, logger);

        sw.Restart();
        await pipeline.RunAsync(sourceDirectory, pipelineDestination, maxParallel: 4, ct);
        sw.Stop();

        await logger.SaveAsync(ct);
        Console.WriteLine($"[Pipeline] Готово за {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"[Pipeline] JSON-журнал: {logPath}");
    }

    private static string? PromptSource()
    {
        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "demo-source");
        Console.Write($"Каталог-источник [{fallback}]: ");
        var input = Console.ReadLine();
        var path = string.IsNullOrWhiteSpace(input) ? fallback : input.Trim();

        if (!Directory.Exists(path))
        {
            Console.WriteLine($"[Info] Каталог не найден, создаём демо-файлы: {path}");
            Directory.CreateDirectory(path);
            for (var i = 1; i <= 5; i++)
            {
                var filePath = Path.Combine(path, $"file{i}.txt");
                File.WriteAllText(filePath, $"Файл номер {i}\n{new string('x', i * 100)}");
            }
        }

        return path;
    }

    private static string? PromptDestination()
    {
        var fallback = Path.Combine(Directory.GetCurrentDirectory(), "demo-backup");
        Console.Write($"Каталог-назначение [{fallback}]: ");
        var input = Console.ReadLine();
        return string.IsNullOrWhiteSpace(input) ? fallback : input.Trim();
    }
}