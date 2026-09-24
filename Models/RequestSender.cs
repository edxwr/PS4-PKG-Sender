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
            throw new("Invalid PS4 IP");
        }
        else if (IP.AddressFamily != AddressFamily.InterNetwork)
        {
            throw new("Not IPv4 IP Address");
        }
        
        await client.ConnectAsync(ps4IP, 9090);
        Console.Write("Connected to PS4");
    }

    async public Task SendInstallCommand(string pcIP, string pkgName)
    {
        System.Console.WriteLine($"Sending \"{pkgName}\" Install Command");

        NetworkStream stream = client.GetStream();
        InstallRequest request = new() { Url = $"http://{pcIP}:7777/{pkgName}" };
        byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
        await stream.WriteAsync(bytes);
        await stream.FlushAsync();

        client.Close(); // REFACTOR SA SE INCHIDA CLIENTUL DUPA MESAJ

        System.Console.WriteLine("Sent install command");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        client.Close();
        client.Dispose();
    }
}