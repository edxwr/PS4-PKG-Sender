using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ps4PkgSender.Models.Structs;

public class RequestSender : IDisposable
{
    private readonly HttpClient client = new();

    public RequestSender(string ps4IP)
    {
        this.client.BaseAddress = new Uri(ps4IP + ":12800");
    }

    public async Task SendInstallCommand(string pcIP, string pkgName)
    {
        System.Console.WriteLine($"Sending \"{pkgName}\" Install Command");

        InstallRequest request = new();
        request.Packages = [$"http://{pcIP}:7777/{pkgName}"];

        using var response = await client.PostAsJsonAsync("api/install", request);

        System.Console.WriteLine("Sent install command");
    }

    public async Task<TaskID> GetTaskID(string contentID, PKGType subtype)
    {
        System.Console.WriteLine($"Getting {contentID} taskID");

        TaskRequest request = new()
        {
            ContentID = contentID,
            Subtype = subtype switch
            {
                PKGType.Game => 6,
                PKGType.DLC => 7,
                PKGType.Update => 8,
                _ => 9
            }
        };

        using var response = await client.PostAsJsonAsync("/api/find_task", request);
        TaskID taskID = await response.Content.ReadFromJsonAsync<TaskID>() ?? new TaskID();
        return taskID;
    }

    public async Task SendPauseCommand(TaskID taskID)
    {
        System.Console.WriteLine("Pausing current transfer");

        await client.PostAsJsonAsync("/api/pause_task", taskID);
    }

    public async Task SendCancelCommand(TaskID taskID)
    {
        System.Console.WriteLine("Cancelling current transfer");

        await client.PostAsJsonAsync("/api/stop_task", taskID);
    }

    public async Task SendResumeCommand(TaskID taskID)
    {
        System.Console.WriteLine("Resuming current transfer");

        await client.PostAsJsonAsync("/api/resume_task", taskID);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        client.Dispose();
    }
}