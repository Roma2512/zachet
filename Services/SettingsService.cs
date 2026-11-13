using Microsoft.Win32;

namespace Variant05.Services;

public sealed class SettingsService
{
    private const string KeyPath = @"Software\Variant05\BackupManager";
    private const string ValueName = "DestinationDirectory";

    // TODO 7: Сохранять каталог назначения в HKCU (чтение).
    public string? LoadDestination()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("[Settings] HKCU доступен только в Windows, пропускаем чтение.");
            return null;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return key?.GetValue(ValueName) as string;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Settings] Не удалось прочитать HKCU: {ex.Message}");
            return null;
        }
    }

    // TODO 7: Сохранять каталог назначения в HKCU (запись).
    public void SaveDestination(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("[Settings] HKCU доступен только в Windows, пропускаем запись.");
            return;
        }

        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(KeyPath);
            key.SetValue(ValueName, path);
            Console.WriteLine($"[Settings] Каталог сохранён в HKCU: {path}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Settings] Не удалось записать HKCU: {ex.Message}");
        }
    }
}