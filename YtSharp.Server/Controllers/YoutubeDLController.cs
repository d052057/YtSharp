using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
namespace YtSharp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeDLController : ControllerBase
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly Dictionary<string, DownloadStatus> _downloads;

        public YoutubeDLController()
        {
            _youtubeDL = new YoutubeDL { 
                YoutubeDLPath = "yt-dlp.exe",
                OutputFolder = @"c:\medias\poster",
                FFmpegPath = "ffmpeg.exe"
            };
            //_downloads = new Dictionary<string, DownloadStatus>();
            _downloads = [];
        }

        // Model to receive download requests
        public class DownloadRequest
        {
            public string? Url { get; set; }
            public bool AudioOnly { get; set; }
            public string? Options { get; set; }
        }

        // Model to track download status
        public class DownloadStatus
        {
            public string? Id { get; set; }
            public string? Url { get; set; }
            public string? State { get; set; }
            public double Progress { get; set; }
            public string? DownloadSpeed { get; set; }
            public string? ETA { get; set; }
            public List<string> Output { get; set; } = new List<string>();
            public bool IsCompleted { get; set; }
            public bool IsSuccessful { get; set; }
            public string? FilePath { get; set; }
            public string? ErrorMessage { get; set; }
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
                return BadRequest("URL is required");

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
            _downloads[downloadId] = downloadStatus;

            // Start download process asynchronously
            await Task.Run(async () =>
            {
                try
                {
                    // Set up progress tracking
                    var progress = new Progress<DownloadProgress>(p =>
                    {
                        downloadStatus.State = p.State.ToString();
                        downloadStatus.Progress = p.Progress;
                        downloadStatus.DownloadSpeed = p.DownloadSpeed;
                        downloadStatus.ETA = p.ETA;
                    });

                    var output = new Progress<string>(s =>
                    {
                        downloadStatus.Output.Add(s);
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
                }
                catch (Exception ex)
                {
                    downloadStatus.IsCompleted = true;
                    downloadStatus.IsSuccessful = false;
                    downloadStatus.ErrorMessage = ex.Message;
                }
            });

            // Return the download ID for status tracking
            return Ok(new { DownloadId = downloadId });
        }

        [HttpGet("status/{id}")]
        public IActionResult GetDownloadStatus(string id)
        {
            if (!_downloads.TryGetValue(id, out var status))
            {
                return NotFound("Download not found");
            }

            return Ok(status);
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
        {
            if (string.IsNullOrEmpty(url))
                return BadRequest("URL is required");

            try
            {
                RunResult<VideoData> result = await _youtubeDL.RunVideoDataFetch(url);

                if (result.Success)
                {
                    return Ok(result.Data);
                }
                else
                {
                    return BadRequest(new { Error = string.Join("\n", result.ErrorOutput) });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}

