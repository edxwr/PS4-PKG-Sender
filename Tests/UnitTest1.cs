using System.Net;
using Ps4PkgSender.Models;
using Ps4PkgSender.Models.Structs;

namespace Tests;

public class UnitTest1
{
    string pcIP = Dns.GetHostEntry(String.Empty).AddressList.First(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToString();
    const string ps4IP = "192.168.0.162";
    const string fileName = "[DLPSGAME.COM]-Commander.Cherrys.Puzzled.Journey_CUSA02889_v1.00_[5.05]_OPOISSO893.pkg";
    const string path = "/home/edward/Downloads/[DLPSGAME.COM]-C_02889_v1.00_[5.05]_OPOISSO893/[DLPSGAME.COM]-Commander.Cherrys.Puzzled.Journey_CUSA02889_v1.00_[5.05]_OPOISSO893.pkg";
    Dictionary<string, string> pkgPaths = [];

    [Fact]
    public async Task Test()
    {
        pkgPaths.Add(fileName, path);

        using RequestSender sender = new();
        using PkgServer httpServer = new(pcIP);

        try
        {   
            httpServer.StartServing(pkgPaths);
        } catch (Exception e)
        {
            System.Console.WriteLine($"Error starting http server: {e}");
            httpServer.StopServing();
            return;
        }
        
        try
        {
            await sender.ConnectToPs4(ps4IP);
        } catch (Exception e)
        {
            System.Console.WriteLine($"Error connecting to the PS4: {e}");
            httpServer.StopServing();
            return;
        }
        System.Console.WriteLine("Connected to ps4");

        var tcs = new TaskCompletionSource();
        void OnTransferDone(){
            tcs.TrySetResult();
        }
        httpServer.FileTransferred += OnTransferDone;
        await sender.SendInstallCommand(pcIP, fileName);
        await tcs.Task;
        httpServer.FileTransferred -= OnTransferDone;

        httpServer.StopServing();
        System.Console.WriteLine("All files transferred");
    }
}