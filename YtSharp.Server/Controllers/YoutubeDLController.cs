using Microsoft.AspNetCore.Mvc;
using System.Collections.Concurrent;
using YoutubeDLSharp;
using static YtSharp.Server.Models.YtSharpModel;
using YtSharp.Server.services;
namespace YtSharp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeDLController : ControllerBase
    {
        private readonly YoutubeDL _youtubeDL;
        private readonly ConcurrentDictionary<string, DownloadStatus> _downloads;
        private readonly IYtSharpService _youtubeDLService;
        public YoutubeDLController(IYtSharpService ytSharpService)
        {
            _youtubeDL = new YoutubeDL { 
                YoutubeDLPath = "yt-dlp.exe",
                OutputFolder = @"c:\medias\poster",
                FFmpegPath = "ffmpeg.exe"
            };
            _downloads = [];
            _youtubeDLService = ytSharpService;
        }



        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            if (string.IsNullOrEmpty(request.Url))
                return BadRequest("URL is required");
            try
            {
                string downloadId = await _youtubeDLService.StartDownload(request);
                return Ok(new { DownloadId = downloadId });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }

        [HttpGet("status/{id}")]
        public IActionResult GetDownloadStatus(string id)
        {
            var status = _youtubeDLService.GetDownloadStatus(id);

            if (status == null)
            {
                return NotFound("Download not found");
            }

            return Ok(status);
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
        {
            try
            {
                var videoData = await _youtubeDLService.GetVideoInfo(url);
                return Ok(videoData);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = ex.Message });
            }
        }
    }
}

