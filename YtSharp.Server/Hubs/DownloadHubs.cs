using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using YoutubeDLSharp;
using YoutubeDLSharp.Metadata;
using YoutubeDLSharp.Options;
using YtSharp.Server.Hubs;
using System.Threading.Tasks;
using YtSharp.Server.Service;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using YtSharp.Server.Models;
using Microsoft.AspNetCore.JsonPatch.Operations;
namespace YtSharp.Server.Hubs
{
    public class DownloadHub : Microsoft.AspNetCore.SignalR.Hub
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IDownloadService _downloadService;
        private readonly string _apiBaseUrl;
        public DownloadHub(
            IHttpClientFactory httpClientFactory,
            IDownloadService downloadService,
            IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _downloadService = downloadService;
            //_apiBaseUrl = configuration.GetSection("ApiSettings")["BaseUrl"] ?? @"https://localhost:7217";

            _apiBaseUrl = configuration.GetSection("ApiSettings")["BaseUrl"] ?? throw new ArgumentNullException("BaseUrl configuration is missing");
        }
        public string GetConnectionId()
        {
            return Context.ConnectionId;
        }
        public async Task HubStartDownloadServiceAsync(DownloadRequest request)
        {
            string conn = request.DownloadId;
           
            try
            {
                Func<DownloadProgress, Task> callback = async p =>
                {
                    // Send progress updates to the client
                    await Clients.Client(conn).SendAsync("ReceiveProgress", p.Progress * 100);
                    switch (p.State)
                    {
                        case DownloadState.Error:
                            {
                                await Clients.Client(conn).SendAsync("ReceiveState", "Error");
                                break;
                            }
                        case DownloadState.Downloading:
                            {
                                await Clients.Client(conn).SendAsync("ReceiveState", "Downloading");
                                break;
                            }
                        case DownloadState.Success:
                            {
                                await Clients.Client(conn).SendAsync("ReceiveState", "Success");
                                break;
                            }
                        case DownloadState.PostProcessing:
                            {
                                await Clients.Client(conn).SendAsync("ReceiveState", "Post Processing");
                                break;
                            }
                        case DownloadState.PreProcessing:
                            {
                                await Clients.Client(conn).SendAsync("ReceiveState", "Pre Processing");
                                break;
                            }
                        //case DownloadState.None:
                        default:
                            {
                                await Clients.Client(conn).SendAsync("ReceiveState", "None");
                                break;
                            }
                    }
                    await Clients.Client(conn).SendAsync("ReceiveSpeed", p.DownloadSpeed ?? "");
                    await Clients.Client(conn).SendAsync("ReceiveETA", p.ETA ?? "");
                    await Clients.Client(conn).SendAsync("ReceiveTotalSize", p.TotalDownloadSize ?? ""  );
                    await Clients.Client(conn).SendAsync("ReceiveVideoIndex", p.VideoIndex);
                    await Clients.Client(conn).SendAsync("ReceiveData", p.Data ?? "");
                };
                await _downloadService.StartDownloadAsync(request, callback);
                await Clients.Client(conn).SendAsync("DownloadFinished", $"Hub Download complete! Files saved to {request.OutputFolder}.");
            }
            catch (UriFormatException)
            {
                await Clients.Client(conn).SendAsync("ReceiveError", "Hub The URL format is invalid.");
            }
            catch (IOException)
            {
                await Clients.Client(conn).SendAsync("ReceiveError", "Hub An error occurred while accessing the file system.");
            }
            catch (Exception ex)
            {
                await Clients.Client(conn).SendAsync("ReceiveError", $"Hub Error during download: {ex.Message}");
            }
        }
    }
}
