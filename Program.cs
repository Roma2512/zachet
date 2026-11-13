using Variant05.Services;
using Variant05.Models;
namespace Variant05;

internal static class Program
{
    private static async Task Main()
    {
        // TODO: перенесите предметную логику в отдельные методы и классы.
        // TODO: не размещайте весь проект в Main.
        // TODO 1: Получить список файлов исходного каталога.
        // TODO 2: Реализовать последовательное копирование.
        // TODO 3: Реализовать асинхронное копирование потоками.
        // TODO 4: Ограничить параллельные копирования через SemaphoreSlim.
        // TODO 5: Проверять целостность копии через SHA256.
        // TODO 6: Обрабатывать ошибки каждого файла и закрывать потоки.
        // TODO 7: Сохранять каталог назначения в HKCU.

        // TODO 8: Построить pipeline обнаружение → копирование → проверка через Channel.
        // TODO 9: Добавить JSON-журнал.
        // TODO 10: Добавить запуск внешней утилиты и ожидание её завершения через Process.
        
        Console.WriteLine("Зачётный проект 05: Менеджер резервного копирования");
        Console.WriteLine(new string('=', 64));

        using var cts = new CancellationTokenSource();

        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        var service = new ProjectService();
        await service.RunAsync(cts.Token);

        Console.WriteLine();
        Console.WriteLine("Нажмите Enter для завершения.");
        Console.ReadLine();
    }
}