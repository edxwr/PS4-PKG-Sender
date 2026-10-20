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
    private HttpClient client = new();

    public RequestSender(string ps4IP)
    {
        this.client.BaseAddress = new Uri(ps4IP + ":12800");
    }

    async public Task SendInstallCommand(string pcIP, string pkgName)
    {
        System.Console.WriteLine($"Sending \"{pkgName}\" Install Command");

        InstallRequest request = new();
        request.Packages = [$"http://{pcIP}:7777/{pkgName}"];

        using var response = await client.PostAsJsonAsync("api/install", request);

        System.Console.WriteLine("Sent install command");
    }

    async public Task<TaskID> GetTaskID(string contentID, PKGType subtype)
    {
        System.Console.WriteLine($"Getting {contentID} taskID");

        TaskRequest request = new();
        request.ContentID = contentID;
        request.Subtype = subtype switch
        {
            PKGType.Game => 6,
            PKGType.DLC => 7,
            PKGType.Update => 8,
            _ => 9
        };

        using var response = await client.PostAsJsonAsync("/api/find_task", request);
        TaskID taskID = await response.Content.ReadFromJsonAsync<TaskID>() ?? new TaskID();
        return taskID;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        client.Dispose();
    }
}