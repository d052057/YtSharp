using Microsoft.AspNetCore.SignalR;
using static YtSharp.Server.Models.YtSharpModel;
using System.Threading.Tasks;
namespace YtSharp.Server.Hubs
{
    public class DownloadHub : Hub
    {
        public async Task SendProgress(string downloadId, DownloadStatus status)
        {
            await Clients.All.SendAsync("ReceiveProgress", downloadId, status);
        }
    }
}
