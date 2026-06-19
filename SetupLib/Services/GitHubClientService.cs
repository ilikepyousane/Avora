using SetupLib.Interfaces;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace SetupLib.Services
{
    public class GitHubClientService : IGitHubClientService
    {
        private const string GitHubOwner = "ilikepyousane";
        private const string GitHubRepo = "Avora";

        public async Task<ReleaseInfo> GetLatestReleaseInfo(string owner, string repo, string currentVersion)
        {
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Add("User-Agent", "Avora");
            http.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

            var response = await http.GetAsync(
                $"https://api.github.com/repos/{GitHubOwner}/{GitHubRepo}/releases/latest");
            var bytes = await response.Content.ReadAsByteArrayAsync();
            var json = System.Text.Encoding.UTF8.GetString(bytes);

            var doc = JsonDocument.Parse(json);
            var tag = (doc.RootElement.GetProperty("tag_name").GetString() ?? "").TrimStart('v');

            if (CompareVersions(tag, currentVersion) <= 0)
                return new ReleaseInfo { IsNewVersionAvailable = false };

            string osArch = RuntimeInformation.OSArchitecture switch
            {
                Architecture.X64 => "x64",
                Architecture.Arm64 => "ARM64",
                Architecture.X86 => "x86",
                _ => "x64"
            };

            string? zipUrl = null;
            long zipSize = 0;

            var assets = doc.RootElement.GetProperty("assets");
            foreach (var asset in assets.EnumerateArray())
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
                {
                    zipUrl = asset.GetProperty("browser_download_url").GetString();
                    zipSize = asset.GetProperty("size").GetInt64();
                }
            }

            if (string.IsNullOrEmpty(zipUrl))
                return new ReleaseInfo { IsNewVersionAvailable = false };

            return new ReleaseInfo
            {
                IsNewVersionAvailable = true,
                Version = tag,
                Name = doc.RootElement.TryGetProperty("name", out var n) ? n.GetString() ?? $"Avora {tag}" : $"Avora {tag}",
                Body = doc.RootElement.TryGetProperty("body", out var b) ? b.GetString() ?? "" : "",
                Assets = new Dictionary<PackageType, PackageAsset>
                {
                    [PackageType.ZIP] = new PackageAsset
                    {
                        Url = zipUrl,
                        Size = (int)zipSize,
                        Architecture = osArch
                    }
                },
                CertificateUrl = ""
            };
        }

        private static int CompareVersions(string v1, string v2)
        {
            if (v1 == v2) return 0;
            if (v1 == null) return -1;
            if (v2 == null) return 1;

            v1 = v1.TrimStart('v', 'V');
            v2 = v2.TrimStart('v', 'V');

            var p1 = v1.Split('.');
            var p2 = v2.Split('.');
            int max = Math.Max(p1.Length, p2.Length);
            for (int i = 0; i < max; i++)
            {
                int a = i < p1.Length && int.TryParse(p1[i], out var parsedA) ? parsedA : 0;
                int b = i < p2.Length && int.TryParse(p2[i], out var parsedB) ? parsedB : 0;
                if (a != b) return a.CompareTo(b);
            }
            return 0;
        }
    }
}
