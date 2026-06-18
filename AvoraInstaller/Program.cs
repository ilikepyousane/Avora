using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;

namespace AvoraInstaller;

class Program
{
    private const string GitHubOwner = "ilikepyousane";
    private const string GitHubRepo = "Avora";
    private const string AppName = "Avora";
    private static readonly string InstallPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), AppName);
    private static readonly string TempPath = Path.Combine(Path.GetTempPath(), "AvoraSetup");

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "Avora - Установка";

        PrintHeader();

        if (!IsRunningAsAdministrator())
        {
            PrintError("Для установки необходимы права администратора.");
            PrintInfo("Запустите установщик от имени администратора.");
            WaitForExit();
            return;
        }

        Directory.CreateDirectory(TempPath);

        // Step 1: Check .NET Runtime
        PrintStep(1, 4, "Проверка .NET Desktop Runtime 8.0...");
        if (!CheckDotNetRuntime())
        {
            PrintWarning(".NET Desktop Runtime 8.0 не найден.");
            await InstallDotNetRuntime();
        }
        else
        {
            PrintOk(".NET Desktop Runtime 8.0 установлен.");
        }

        // Step 2: Check Windows App Runtime
        PrintStep(2, 4, "Проверка Windows App Runtime...");
        if (!CheckWindowsAppRuntime())
        {
            PrintWarning("Windows App Runtime не найден.");
            await InstallWindowsAppRuntime();
        }
        else
        {
            PrintOk("Windows App Runtime установлен.");
        }

        // Step 3: Download Avora
        PrintStep(3, 4, "Загрузка Avora...");
        var zipPath = await DownloadAvora();
        if (zipPath == null)
        {
            PrintError("Не удалось загрузить Avora.");
            WaitForExit();
            return;
        }
        PrintOk("Avora загружен.");

        // Step 4: Install Avora
        PrintStep(4, 4, "Установка Avora...");
        await InstallAvora(zipPath);
        PrintOk("Avora установлен в: " + InstallPath);

        // Cleanup
        try { Directory.Delete(TempPath, true); } catch { }

        // Create shortcut
        CreateDesktopShortcut();

        Console.WriteLine();
        PrintOk("Установка завершена!");
        Console.WriteLine();

        var launch = AskQuestion("Запустить Avora сейчас? (y/n): ");
        if (launch.ToLower() == "y")
        {
            var exePath = FindAvoraExe();
            if (exePath != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    UseShellExecute = true
                });
            }
        }
    }

    static void PrintHeader()
    {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║         Avora - Установщик v0.1.0       ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.ResetColor();
        Console.WriteLine();
    }

    static void PrintStep(int current, int total, string message)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"[{current}/{total}] ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    static void PrintOk(string message)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write("  ✓ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    static void PrintWarning(string message)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write("  ⚠ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    static void PrintError(string message)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.Write("  ✗ ");
        Console.ResetColor();
        Console.WriteLine(message);
    }

    static void PrintInfo(string message)
    {
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("  " + message);
        Console.ResetColor();
    }

    static string AskQuestion(string question)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("  " + question);
        Console.ResetColor();
        return Console.ReadLine() ?? "";
    }

    static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    static bool CheckDotNetRuntime()
    {
        try
        {
            var result = RunCommand("dotnet", "--list-runtimes");
            return result.Contains("Microsoft.WindowsDesktop.App 8.");
        }
        catch
        {
            return false;
        }
    }

    static bool CheckWindowsAppRuntime()
    {
        try
        {
            var result = RunCommand("powershell.exe",
                "-Command \"Get-AppxPackage -Name *WindowsAppRuntime* | Select-Object -First 1\"");
            return !string.IsNullOrWhiteSpace(result);
        }
        catch
        {
            return false;
        }
    }

    static async Task InstallDotNetRuntime()
    {
        PrintInfo("Скачивание .NET Desktop Runtime 8.0...");

        var arch = RuntimeInformation.OSArchitecture;
        string url = arch switch
        {
            Architecture.X64 => "https://download.visualstudio.microsoft.com/download/pr/907765b0-2bf7-4e21-a054-4c5d4e4c123e/a534614133f6e3b2e3a2c9b5b3c5e8c0/windowsdesktop-runtime-8.0.11-win-x64.exe",
            Architecture.Arm64 => "https://download.visualstudio.microsoft.com/download/pr/907765b0-2bf7-4e21-a054-4c5d4e4c123e/a534614133f6e3b2e3a2c9b5b3c5e8c0/windowsdesktop-runtime-8.0.11-win-arm64.exe",
            _ => throw new Exception("Неподдерживаемая архитектура")
        };

        var installerPath = Path.Combine(TempPath, "dotnet-runtime.exe");
        await DownloadFile(url, installerPath);

        PrintInfo("Установка .NET Desktop Runtime 8.0...");
        RunCommand(installerPath, "/install /quiet /norestart");
        PrintOk(".NET Desktop Runtime 8.0 установлен.");
    }

    static async Task InstallWindowsAppRuntime()
    {
        PrintInfo("Скачивание Windows App Runtime...");

        var arch = RuntimeInformation.OSArchitecture;
        string url = arch switch
        {
            Architecture.X64 => $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/download/0.1.0/WindowsAppRuntimeInstall.1.6.3-x64.exe",
            Architecture.Arm64 => $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/download/0.1.0/WindowsAppRuntimeInstall.1.6.3-arm64.exe",
            Architecture.X86 => $"https://github.com/{GitHubOwner}/{GitHubRepo}/releases/download/0.1.0/WindowsAppRuntimeInstall.1.6.3-x86.exe",
            _ => throw new Exception("Неподдерживаемая архитектура")
        };

        var installerPath = Path.Combine(TempPath, "WindowsAppRuntime.exe");
        await DownloadFile(url, installerPath);

        PrintInfo("Установка Windows App Runtime...");
        RunCommand(installerPath, "--quiet");
        PrintOk("Windows App Runtime установлен.");
    }

    static async Task<string?> DownloadAvora()
    {
        PrintInfo("Получение информации о последнем релизе...");

        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "AvoraInstaller");

        var releaseJson = await http.GetStringAsync(
            $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest");

        // Find .zip asset URL
        var zipMatch = System.Text.RegularExpressions.Regex.Match(
            releaseJson, @"""browser_download_url""\s*:\s*""([^""]*\.zip)""");

        if (!zipMatch.Success)
        {
            PrintError("ZIP файл не найден в релизе.");
            return null;
        }

        var zipUrl = zipMatch.Groups[1].Value;
        var zipPath = Path.Combine(TempPath, "Avora.zip");

        await DownloadFile(zipUrl, zipPath);
        return zipPath;
    }

    static async Task InstallAvora(string zipPath)
    {
        if (Directory.Exists(InstallPath))
        {
            var confirm = AskQuestion("Avora уже установлена. Переустановить? (y/n): ");
            if (confirm.ToLower() != "y")
            {
                PrintInfo("Установка отменена.");
                return;
            }

            // Kill running processes
            foreach (var proc in Process.GetProcessesByName("Avora"))
            {
                try { proc.Kill(); } catch { }
            }
            await Task.Delay(1000);
        }

        Directory.CreateDirectory(InstallPath);
        ZipFile.ExtractToDirectory(zipPath, InstallPath, overwriteFiles: true);

        // Find and fix exe name if needed
        var exePath = FindAvoraExe();
        if (exePath != null && Path.GetFileName(exePath) != "Avora.exe")
        {
            var targetPath = Path.Combine(InstallPath, "Avora.exe");
            File.Copy(exePath, targetPath, true);
        }
    }

    static string? FindAvoraExe()
    {
        if (!Directory.Exists(InstallPath)) return null;

        var exe = Directory.GetFiles(InstallPath, "*.exe", SearchOption.AllDirectories)
            .FirstOrDefault(f =>
            {
                var name = Path.GetFileNameWithoutExtension(f);
                return name.Contains("Avora", StringComparison.OrdinalIgnoreCase) ||
                       name.Contains("VK", StringComparison.OrdinalIgnoreCase);
            });

        return exe;
    }

    static void CreateDesktopShortcut()
    {
        try
        {
            var exePath = FindAvoraExe();
            if (exePath == null) return;

            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var shortcutPath = Path.Combine(desktopPath, "Avora.lnk");

            Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType == null) return;

            dynamic shell = Activator.CreateInstance(shellType);
            var shortcut = shell.CreateShortcut(shortcutPath);
            shortcut.TargetPath = exePath;
            shortcut.WorkingDirectory = InstallPath;
            shortcut.Description = "Avora Music Player";
            shortcut.Save();

            PrintOk("Ярлык создан на рабочем столе.");
        }
        catch (Exception ex)
        {
            PrintWarning("Не удалось создать ярлык: " + ex.Message);
        }
    }

    static async Task DownloadFile(string url, string destinationPath)
    {
        using var http = new HttpClient();
        http.Timeout = TimeSpan.FromMinutes(10);

        using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var totalRead = 0L;
        var buffer = new byte[8192];

        await using var contentStream = await response.Content.ReadAsStreamAsync();
        await using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write);

        int bytesRead;
        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalRead += bytesRead;

            if (totalBytes > 0)
            {
                var percent = (int)(totalRead * 100 / totalBytes);
                Console.Write($"\r  Загрузка... {percent}% ({totalRead / 1024 / 1024} МБ / {totalBytes / 1024 / 1024} МБ)");
            }
            else
            {
                Console.Write($"\r  Загрузка... {totalRead / 1024 / 1024} МБ");
            }
        }
        Console.WriteLine();
    }

    static string RunCommand(string fileName, string arguments)
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (process == null) return "";

        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    static void WaitForExit()
    {
        Console.WriteLine();
        PrintInfo("Нажмите любую клавишу для выхода...");
        Console.ReadKey(true);
    }
}
