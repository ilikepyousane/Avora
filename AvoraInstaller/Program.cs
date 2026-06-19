using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
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
    private static readonly string TempDir = Path.Combine(Path.GetTempPath(), "AvoraSetup");

    static async Task Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        Console.Title = "Avora - Установка";
        Console.WriteLine("╔══════════════════════════════════════════╗");
        Console.WriteLine("║         Avora - Установщик v0.1.1       ║");
        Console.WriteLine("╚══════════════════════════════════════════╝");
        Console.WriteLine();

        try
        {
            Directory.CreateDirectory(TempDir);

            Console.WriteLine("[1/5] Проверка .NET 8.0 Desktop Runtime...");
            await CheckDotNet();

            Console.WriteLine("[2/5] Проверка Windows App Runtime...");
            await CheckWinAppRuntime();

            Console.WriteLine("[3/5] Скачивание Avora...");
            var zipPath = await DownloadLatestRelease();
            Console.WriteLine("  ✓ Скачано.");

            Console.WriteLine("[4/5] Установка...");
            Install(zipPath);
            Console.WriteLine($"  ✓ Установлено в: {InstallPath}");

            Console.WriteLine("[5/5] Ярлык...");
            CreateShortcut();
            Console.WriteLine("  ✓ Ярлык создан.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✗ Ошибка: {ex.Message}");
        }
        finally
        {
            try { Directory.Delete(TempDir, true); } catch { }
        }

        Console.WriteLine();
        Console.WriteLine("  Установка завершена!");
        Console.Write("  Запустить Avora? (y/n): ");
        if (Console.ReadLine()?.Trim().ToLower() == "y")
        {
            var exe = FindExe();
            if (exe != null) Process.Start(new ProcessStartInfo(exe) { UseShellExecute = true });
        }
    }

    static bool CheckDotNetRuntime()
    {
        try
        {
            var psi = new ProcessStartInfo("dotnet", "--list-runtimes")
            {
                RedirectStandardOutput = true, CreateNoWindow = true, UseShellExecute = false
            };
            var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd();
            proc.WaitForExit();
            return output.Contains("Microsoft.WindowsDesktop.App 8.");
        }
        catch { return false; }
    }

    static async Task CheckDotNet()
    {
        if (CheckDotNetRuntime())
        {
            Console.WriteLine("  ✓ Уже установлен.");
            return;
        }

        Console.WriteLine("  ⚠ Не найден. Скачивание и установка...");
        var version = await GetLatestDotNetVersion();
        Console.WriteLine($"  → Версия: {version}");

        string url = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => $"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/{version}/windowsdesktop-runtime-{version}-win-x64.exe",
            Architecture.Arm64 => $"https://builds.dotnet.microsoft.com/dotnet/WindowsDesktop/{version}/windowsdesktop-runtime-{version}-win-arm64.exe",
            _ => throw new Exception("Неподдерживаемая архитектура")
        };
        await DownloadAndRun(url, "Установка .NET 8.0...");
    }

    static async Task<string> GetLatestDotNetVersion()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "AvoraInstaller");
        var json = await http.GetStringAsync("https://dotnetcli.azureedge.net/dotnet/release-metadata/8.0/releases.json");
        var doc = System.Text.Json.JsonDocument.Parse(json);
        var latest = doc.RootElement.GetProperty("releases")[0];
        return latest.GetProperty("release-version").GetString()!;
    }

    static bool IsWinAppRuntimeInstalled()
    {
        try
        {
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -Command \"Get-AppxPackage -AllUsers *WindowsAppRuntime* | Select-Object -First 1 -ExpandProperty Version\"")
            {
                RedirectStandardOutput = true, CreateNoWindow = true, UseShellExecute = false
            };
            var proc = Process.Start(psi)!;
            var output = proc.StandardOutput.ReadToEnd().Trim();
            proc.WaitForExit();
            return !string.IsNullOrEmpty(output);
        }
        catch { return false; }
    }

    static async Task CheckWinAppRuntime()
    {
        if (IsWinAppRuntimeInstalled())
        {
            Console.WriteLine("  ✓ Уже установлен.");
            return;
        }

        Console.WriteLine("  ⚠ Не найден. Скачивание и установка...");
        var (major, version) = await GetLatestWinAppRuntimeVersion();
        Console.WriteLine($"  → Версия: {version}");

        string url = RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => $"https://aka.ms/windowsappsdk/{major}/{version}/windowsappruntimeinstall-x64.exe",
            Architecture.Arm64 => $"https://aka.ms/windowsappsdk/{major}/{version}/windowsappruntimeinstall-arm64.exe",
            _ => throw new Exception("Неподдерживаемая архитектура")
        };
        await DownloadAndRun(url, "Установка Windows App Runtime...");
    }

    static async Task<(string major, string version)> GetLatestWinAppRuntimeVersion()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "AvoraInstaller");
        var json = await http.GetStringAsync("https://api.github.com/repos/microsoft/WindowsAppSDK/releases?per_page=10");
        var releases = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement[]>(json)!;

        foreach (var release in releases)
        {
            var tag = release.GetProperty("tag_name").GetString()!;
            if (tag.StartsWith("v") && !tag.Contains("exp") && !tag.Contains("preview"))
            {
                var version = tag.TrimStart('v');
                var parts = version.Split('.');
                var major = $"{parts[0]}.{parts[1]}";
                return (major, version);
            }
        }

        throw new Exception("Не удалось найти стабильную версию Windows App Runtime");
    }

    static async Task DownloadAndRun(string url, string description)
    {
        var installerPath = Path.Combine(TempDir, "dep_installer.exe");
        Console.Write($"  Скачивание...");

        using (var http = new HttpClient())
        {
            http.DefaultRequestHeaders.Add("User-Agent", "AvoraInstaller");
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            resp.EnsureSuccessStatusCode();
            await using var stream = await resp.Content.ReadAsStreamAsync();
            await using var file = File.Create(installerPath);
            await stream.CopyToAsync(file);
        }
        Console.WriteLine(" ✓");

        Console.Write($"  Установка...");
        var psi = new ProcessStartInfo(installerPath, "/install /quiet /norestart")
        {
            UseShellExecute = true, Verb = "runas"
        };
        var proc = Process.Start(psi)!;
        await proc.WaitForExitAsync();
        Console.WriteLine(" ✓");
    }

    static async Task<string> DownloadLatestRelease()
    {
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Add("User-Agent", "AvoraInstaller");

        var json = await http.GetStringAsync(
            $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest");
        var doc = System.Text.Json.JsonDocument.Parse(json);

        string? zipUrl = null;
        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
        {
            var name = asset.GetProperty("name").GetString() ?? "";
            if (name.EndsWith(".zip"))
            {
                zipUrl = asset.GetProperty("browser_download_url").GetString();
                break;
            }
        }

        if (zipUrl == null) throw new Exception("ZIP не найден в релизе");

        var zipPath = Path.Combine(TempDir, "Avora.zip");
        Console.Write("  Скачивание...");
        using var resp = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();
        await using var stream = await resp.Content.ReadAsStreamAsync();
        await using var file = File.Create(zipPath);
        await stream.CopyToAsync(file);
        Console.WriteLine(" ✓");

        return zipPath;
    }

    static void Install(string zipPath)
    {
        if (Directory.Exists(InstallPath))
            Directory.Delete(InstallPath, true);
        Directory.CreateDirectory(InstallPath);

        using (var archive = ZipFile.OpenRead(zipPath))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;
                var fullName = entry.FullName;
                var idx = fullName.IndexOf("win-x64");
                if (idx >= 0)
                {
                    var after = fullName.Substring(idx + 7);
                    if (after.StartsWith("/") || after.StartsWith("\\"))
                        after = after.Substring(1);
                    fullName = after;
                }
                if (string.IsNullOrEmpty(fullName)) continue;
                var dest = Path.Combine(InstallPath, fullName);
                Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                using var src = entry.Open();
                using var dst = File.Create(dest);
                src.CopyTo(dst);
            }
        }
    }

    static string? FindExe()
    {
        var exe = Directory.GetFiles(InstallPath, "Avora.exe", SearchOption.AllDirectories);
        return exe.FirstOrDefault();
    }

    static void CreateShortcut()
    {
        try
        {
            var exe = FindExe();
            if (exe == null) return;
            var shell = Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell")!);
            var sc = ((dynamic)shell).CreateShortcut(Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Avora.lnk"));
            sc.TargetPath = exe;
            sc.WorkingDirectory = InstallPath;
            sc.Description = "Avora Music Player";
            sc.Save();
        }
        catch { }
    }
}
