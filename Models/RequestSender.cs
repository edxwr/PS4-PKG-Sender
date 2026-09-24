using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Ps4PkgSender.Models.Structs;

public class RequestSender : IDisposable
{
    private TcpClient client = new();

    async public Task ConnectToPs4(string ps4IP)
    {
        if (IPAddress.TryParse(ps4IP, out IPAddress? IP) == false)
        {
            Console.Write("Invalid PS4 IP");
            return;
        }
        else if (IP.AddressFamily != AddressFamily.InterNetwork)
        {
            Console.Write("Not IPv4 IP Address");
            return;
        }

        try
        {
            await client.ConnectAsync(ps4IP, 9090);
            Console.Write("Connected to PS4");
        } catch
        {
            Console.Write("Error connecting to PS4");
        }
    }

    async public Task SendInstallCommand(string pcIP, string pkgName)
    {
        NetworkStream stream = client.GetStream();
        InstallRequest request = new() { Url = $"http://{pcIP}:7777/{pkgName}" };
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        client.Close();
        client.Dispose();
    }
}