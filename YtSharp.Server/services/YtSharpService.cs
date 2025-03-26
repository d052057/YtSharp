using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using YtSharp.Server.Models;
using Microsoft.AspNetCore.SignalR;
using static YtSharp.Server.Models.YtSharpModel;
using System.Diagnostics;
using System.Data;
using YtSharp.Server.Hubs;

namespace YtSharp.Server.services
{
    public interface IYtSharpService
    {
        Task StartDownload(DownloadRequest request);
        Task<VideoData> GetVideoInfo(string url);
    }

    public class YtSharpService : IYtSharpService
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly IHubContext<DownloadHub> _hubContext;
        private readonly ConcurrentDictionary<string, DownloadStatus> _downloads = new();

        public YtSharpService(IHubContext<DownloadHub> hubContext)
        {
            _youtubeDL = new YoutubeDL
            {
                YoutubeDLPath = "yt-dlp.exe",
                OutputFolder = @"c:\medias\poster",
                FFmpegPath = "ffmpeg.exe"
            };
            _hubContext = hubContext;
        }

        public async Task StartDownload(DownloadRequest request)
        {
            // Create download ID
            string downloadId = request.DownloadId;

            // Initialize download status
            DownloadStatus downloadStatus = new()
            {
                Id = request.DownloadId,
                Url = request.Url,
                State = "Initializing",
                Progress = 0,
                IsCompleted = false
            };

            // Store the download status in memory
            _downloads[downloadId] = downloadStatus;

            // Start download process asynchronously
            await Task.Run(async () =>
            {
                try
                {
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        var updatedStatus = new DownloadStatus
                        {
                            Id = downloadStatus.Id,
                            Url = downloadStatus.Url,
                            State = p.State.ToString(),
                            Progress = p.Progress,
                            DownloadSpeed = p.DownloadSpeed,
                            ETA = p.ETA,
                            Output = downloadStatus.Output,
                            IsCompleted = downloadStatus.IsCompleted,
                            IsSuccessful = downloadStatus.IsSuccessful,
                            FilePath = downloadStatus.FilePath,
                            ErrorMessage = downloadStatus.ErrorMessage
                        };

                        // Update in-memory status
                        _downloads[downloadId] = updatedStatus;

                        // Send progress update to the client via SignalR
                        _ = _hubContext.Clients.All.SendAsync("ReceiveProgress", downloadId, updatedStatus);
                    });

                    var output = new Progress<string>(s =>
                    {
                        // Update output in-memory
                        var status = _downloads[downloadId];
                        status.Output.Add(s);
                        _downloads[downloadId] = status;
                    });

                    // Parse custom options
                    OptionSet? custom = null;
                    if (!string.IsNullOrEmpty(request.Options))
                    {
                        custom = OptionSet.FromString(request.Options.Split('\n'));
                    }

                    // Start download
                    RunResult<string> result;
                    if (request.AudioOnly)
                    {
                        result = await _youtubeDL.RunAudioDownload(
                            request.Url,
                            AudioConversionFormat.Mp3,
                            progress: progress,
                            output: output,
                            overrideOptions: custom
                        );
                    }
                    else
                    {
                        result = await _youtubeDL.RunVideoDownload(
                            request.Url,
                            progress: progress,
                            output: output,
                            overrideOptions: custom
                        );
                    }

                    // Update download status after completion
                    downloadStatus.IsCompleted = true;
                    downloadStatus.IsSuccessful = result.Success;

                    if (result.Success)
                    {
                        downloadStatus.FilePath = result.Data;
                    }
                    else
                    {
                        downloadStatus.ErrorMessage = string.Join("\n", result.ErrorOutput);
                    }

                    // Update in-memory status
                    _downloads[downloadId] = downloadStatus;

                    // Send final update to the client via SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveProgress", downloadId, downloadStatus);
                }
                catch (Exception ex)
                {
                    downloadStatus.IsCompleted = true;
                    downloadStatus.IsSuccessful = false;
                    downloadStatus.ErrorMessage = ex.Message;

                    // Update in-memory status
                    _downloads[downloadId] = downloadStatus;

                    // Send error update to the client via SignalR
                    await _hubContext.Clients.All.SendAsync("ReceiveProgress", downloadId, downloadStatus);

                }
            });
        }

        public async Task<VideoData> GetVideoInfo(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("URL is required");

            RunResult<VideoData> result = await _youtubeDL.RunVideoDataFetch(url);

            if (result.Success)
            {
                return result.Data;
            }

            throw new Exception(string.Join("\n", result.ErrorOutput));
        }
    }
}