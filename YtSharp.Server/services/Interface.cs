using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using static YtSharp.Server.Models.YtSharpModel;
using System.Collections.Concurrent;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using YtSharp.Server.Models;
using Microsoft.AspNetCore.Http.HttpResults;
namespace YtSharp.Server.services
{
    public interface IYtSharpService
    {
        Task<string> StartDownload(DownloadRequest request);
        DownloadStatus? GetDownloadStatus(string id);
        Task<VideoData> GetVideoInfo(string url);
    }
    public class YtSharpService : IYtSharpService
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public YtSharpService(IHttpContextAccessor httpContextAccessor)
        {
            _youtubeDL = new YoutubeDL
            {
                YoutubeDLPath = "yt-dlp.exe",
                OutputFolder = @"c:\medias\poster",
                FFmpegPath = "ffmpeg.exe"
            };
            _httpContextAccessor = httpContextAccessor;
        }

        private Dictionary<string, DownloadStatus> Downloads
        {
            get
            {
                var sessionData = _httpContextAccessor?.HttpContext?.Session.GetString("Downloads");
                if (sessionData != null)
                {
                    // Use null-coalescing operator to handle potential null return
                    return JsonConvert.DeserializeObject<Dictionary<string, DownloadStatus>>(sessionData)
                        ?? [];
                }
                else
                {
                    return [];
                }
            }
            set
            {
                _httpContextAccessor?.HttpContext?.Session.SetString("Downloads", JsonConvert.SerializeObject(value));
            }
        }
        public async Task<string> StartDownload(DownloadRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
                throw new ArgumentException("URL is required");

            // Create download ID
            string downloadId = Guid.NewGuid().ToString();

            // Initialize download status
            var downloadStatus = new DownloadStatus
            {
                Id = downloadId,
                Url = request.Url,
                State = "Initializing",
                Progress = 0,
                IsCompleted = false
            };

            // Store the download status
            //_downloads[downloadId] = downloadStatus;

            var downloads = Downloads;
            downloads[downloadStatus.Id] = downloadStatus;
            Downloads = downloads;


            // Start download process asynchronously
            await Task.Run(async () =>
            {
                try
                {
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        // Create a local copy of downloadStatus and update it
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

                        // Lock the downloads dictionary while updating
                        lock (this)
                        {
                            var downloads = Downloads;
                            downloads[updatedStatus.Id] = updatedStatus;
                            Downloads = downloads;
                        }
                    });

                    var output = new Progress<string>(s =>
                    {
                        lock (this)
                        {
                            var downloads = Downloads;
                            var status = downloads[downloadStatus.Id];
                            status.Output.Add(s);
                            downloads[downloadStatus.Id] = status;
                            Downloads = downloads;
                        }
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
                    // Update final status in session
                    var downloads = Downloads;
                    downloads[downloadStatus.Id] = downloadStatus;
                    Downloads = downloads;
                }
                catch (Exception ex)
                {
                    downloadStatus.IsCompleted = true;
                    downloadStatus.IsSuccessful = false;
                    downloadStatus.ErrorMessage = ex.Message;

                    // Update session in case of error
                    var downloads = Downloads;
                    downloads[downloadStatus.Id] = downloadStatus;
                    Downloads = downloads;
                }
            });

            return downloadId;
        }
        public DownloadStatus? GetDownloadStatus(string id)
        {
            var downloads = Downloads;
            if (downloads == null) return null;

            downloads.TryGetValue(id, out var status);
            return status;
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