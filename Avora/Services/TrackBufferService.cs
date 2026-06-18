using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Avora.Services
{
    public class TrackBufferService
    {
        private static readonly HttpClient _httpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(30),
            DefaultRequestHeaders = { { "Connection", "keep-alive" } }
        };
        private static readonly Dictionary<string, string> _buffer = new();
        private static readonly SemaphoreSlim _downloadSemaphore = new(3, 3);
        private static CancellationTokenSource _cts = new();
        private static string _currentTrackId;
        private static readonly string _cacheDir = Path.Combine(Path.GetTempPath(), "AvoraBuffer");

        public static int MaxForward { get; set; } = 3;
        public static int MaxBackward { get; set; } = 3;

        public static void CancelAll()
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }

        public static string GetBufferedPath(string trackId)
        {
            if (_buffer.TryGetValue(trackId, out var path) && File.Exists(path))
            {
                return path;
            }
            return null;
        }

        public static async Task FillBufferAsync(Func<int, (string id, Uri url)> getTrackById, int currentIndex, int totalCount)
        {
            CancelAll();
            var token = _cts.Token;

            var indices = new List<int>();

            for (int i = 1; i <= MaxForward; i++)
            {
                int idx = currentIndex + i;
                if (idx >= totalCount) break;
                indices.Add(idx);
            }

            for (int i = 1; i <= MaxBackward; i++)
            {
                int idx = currentIndex - i;
                if (idx < 0) break;
                indices.Add(idx);
            }

            foreach (var idx in indices)
            {
                if (token.IsCancellationRequested) break;
                var (id, url) = getTrackById(idx);
                if (!string.IsNullOrEmpty(id) && url != null && !_buffer.ContainsKey(id))
                {
                    _ = BufferTrackAsync(id, url, token);
                    await Task.Delay(10, token).ConfigureAwait(false);
                }
            }
        }

        private static async Task BufferTrackAsync(string trackId, Uri url, CancellationToken token)
        {
            if (_buffer.ContainsKey(trackId)) return;

            await _downloadSemaphore.WaitAsync(token);
            try
            {
                if (_buffer.ContainsKey(trackId)) return;

                Directory.CreateDirectory(_cacheDir);
                var filePath = Path.Combine(_cacheDir, $"{trackId}.tmp");
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, token);
                response.EnsureSuccessStatusCode();

                using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 16384, true))
                {
                    await response.Content.CopyToAsync(fs, token);
                }

                _buffer[trackId] = filePath;
            }
            catch (OperationCanceledException) { }
            catch { }
            finally
            {
                _downloadSemaphore.Release();
            }
        }

        public static void Cleanup(string keepTrackId = null)
        {
            var toRemove = _buffer.Keys.Where(k => k != keepTrackId && k != _currentTrackId).ToList();

            if (toRemove.Count > 6)
            {
                foreach (var key in toRemove.Skip(6))
                {
                    if (_buffer.TryGetValue(key, out var path))
                    {
                        try { File.Delete(path); } catch { }
                        _buffer.Remove(key);
                    }
                }
            }
        }

        public static void SetCurrentTrack(string trackId)
        {
            _currentTrackId = trackId;
        }
    }
}
