using Microsoft.AspNetCore.Mvc;
using YoutubeDLSharp;
using static YtSharp.Server.Models.YtSharpModel;
using YtSharp.Server.services;
namespace YtSharp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeDLController : ControllerBase
    {
        private readonly IYtSharpService _ytSharpService;
        public YoutubeDLController(IYtSharpService ytSharpService)
        {
            _ytSharpService = ytSharpService;
        }

        [HttpPost("download")]
        public async Task<IActionResult> Download([FromBody] DownloadRequest request)
        {
            try
            {
                string downloadId = await _ytSharpService.StartDownload(request);
                return Ok(new { DownloadId = downloadId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("info")]
        public async Task<IActionResult> GetVideoInfo([FromQuery] string url)
        {
            try
            {
                var videoInfo = await _ytSharpService.GetVideoInfo(url);
                return Ok(videoInfo);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}

