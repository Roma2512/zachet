using Variant05.Services;
using Variant05.Models;
namespace Variant05;

internal static class Program
{
    private static async Task Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
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