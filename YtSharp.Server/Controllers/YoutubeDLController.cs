using Microsoft.AspNetCore.Mvc;
using YtSharp.Server.Service;
using YtSharp.Server.Hubs;
using YtSharp.Server.Models;
using Microsoft.AspNetCore.SignalR;
using YoutubeDLSharp;
namespace YtSharp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class YoutubeDLController : ControllerBase
    {
        private readonly IDownloadService _downloadService;
        private readonly IHubContext<DownloadHub> _hubContext;
        public YoutubeDLController(
            IDownloadService downloadService,
            IHubContext<DownloadHub> hubContext)
        {
            _downloadService = downloadService;
            _hubContext = hubContext;
        }


        [HttpPost("start-download")]
        public async Task<IActionResult> StartDownloadAsync([FromBody] DownloadRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Url))
            {
                return BadRequest("URL cannot be empty.");
            }
            if (string.IsNullOrWhiteSpace(request.OutputFolder))
            {
                return BadRequest("Output folder cannot be empty.");
            }
            if (string.IsNullOrEmpty(request.DownloadId))
            {
                return BadRequest("Connection ID is required.");
            }
            await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveProgress", "Controller Download started...");
            try
            {
                // Call the download service and pass a callback function to handle progress
                Func<DownloadProgress, Task> callback = async p =>
                {
                    //            public enum DownloadState
                    //{
                    //    None,
                    //    PreProcessing,
                    //    Downloading,
                    //    PostProcessing,
                    //    Error,
                    //    Success
                    //}

                    // Send progress updates to the client
                    await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveProgress", p.Progress);
                    switch (p.State)
                    {
                        case DownloadState.Error:
                            {
                                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveState", "Error");
                                break;
                            }
                        case DownloadState.Downloading:
                            {
                                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveState", "Downloading");
                                break;
                            }
                        case DownloadState.Success:
                            {
                                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveState", "Success");
                                break;
                            }
                        case DownloadState.PostProcessing:
                            {
                                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveState", "Post Processing");
                                break;
                            }
                        case DownloadState.PreProcessing:
                            {
                                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveState", "Pre Processing");
                                break;
                            }
                        //case DownloadState.None:
                        default:
                            {
                                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveState", "None");
                                break;
                            }
                    }


                    await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveSpeed", p.DownloadSpeed ?? "");
                    await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveETA", p.ETA ?? "");
                    await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveTotalSize", p.TotalDownloadSize ?? "");
                    await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveVideoIndex", p.VideoIndex );
                    await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveData", p.Data ?? "");

                };
                await _downloadService.StartDownloadAsync(request, callback);
                // Notify the client that the download is complete
                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveProgress", $"Hub Download complete! Files saved to {request.OutputFolder}.");
                return Ok("Controller Download successfully completed!");
            }
            catch (Exception ex)
            {
                // Notify the client about the error
                await _hubContext.Clients.Client(request.DownloadId).SendAsync("ReceiveError", $"Controller Error during download: {ex.Message}");
                return StatusCode(500, "Error during download.");
            }
        }
    }
}
