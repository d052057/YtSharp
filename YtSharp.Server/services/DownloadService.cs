using Microsoft.AspNetCore.SignalR;
using System;
using System.Diagnostics;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using YtSharp.Server.Hubs;
using System.Text.RegularExpressions;
using YtSharp.Server.Models;
using System.IO;
namespace YtSharp.Server.Service
{
    public interface IDownloadService
    {
        Task StartDownloadAsync(DownloadRequest request, Func<DownloadProgress, Task> callback);
    };
    public class DownloadService : IDownloadService
    {
        private readonly IHubContext<DownloadHub> _hubContext;
        private readonly YoutubeDL _youtubeDL;
        private readonly ILogger<DownloadService> _logger;

        public DownloadService(IHubContext<DownloadHub> hubContext, ILogger<DownloadService> logger)
        {
            _logger = logger;
            _hubContext = hubContext;
            _youtubeDL = new YoutubeDL
            {
                YoutubeDLPath = "yt-dlp.exe",
                FFmpegPath = "ffmpeg.exe",
                OutputFolder = @"c:\medias\poster"
            };
        }
        public async Task StartDownloadAsync(DownloadRequest request, Func<DownloadProgress, Task> callback)
        {
            var progress = new Progress<DownloadProgress>(async p =>
            {
                if (callback != null)
                {
                    await callback(p); // Invoke the callback with the progress update
                }
            });
            // a cancellation token source used for cancelling the download
            // use `cts.Cancel();` to perform cancellation
            var cts = new CancellationTokenSource();
            // ...
            try
            {
                _logger.LogInformation("Running try _youtubeDL.RunVideoDownload");
                await _youtubeDL.RunVideoDownload(request.Url,
                                            progress: progress, ct: cts.Token);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RunVideoDownload: {ex.Message}");
                _logger.LogError($"Error in RunVideoDownload: {ex.Message}");
                throw;
            }

        }
    }
}
