using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Ps4PkgSender.Models.Structs;

namespace Ps4PkgSender.Models;

public class InfoSender(string localIp, string infoSenderPort, string pkgServerPort, List<PKGInfo> Pkgs) : IDisposable
{
    private readonly TcpListener listener = new(IPAddress.Parse(localIp), int.Parse(infoSenderPort));
    private readonly List<PKGInfo> pkgs = Pkgs;
    private readonly string localIp = localIp;
    private readonly string pkgServerPort = pkgServerPort;
    public void StartServing()
    {
        listener.Start();
        Task.Run(SendPkgsInfo);
    }

    private async Task SendPkgsInfo()
    {
        try
        {
            foreach (PKGInfo pkg in pkgs)
            {
                using TcpClient ps4 = await listener.AcceptTcpClientAsync();
                System.Console.WriteLine("PS4 Connected to InfoSender");
                await SendPkgInfo(pkg, ps4);
            }

            using TcpClient client = await listener.AcceptTcpClientAsync();
            using BinaryWriter writer = new(client.GetStream());
            writer.Write((uint)0);
        } finally
        {
            listener.Stop();
        }
    }

    private async Task SendPkgInfo(PKGInfo pkg, TcpClient ps4)
    {
        using NetworkStream stream = ps4.GetStream();
        using MemoryStream buffer = new();
        using BinaryWriter writer = new(buffer);

        writer.Write((uint)1);
        WriteSizePrefixedString(writer, $"http://{localIp}:{pkgServerPort}/{Uri.EscapeDataString(pkg.FileName)}");
        WriteSizePrefixedString(writer, pkg.Title);
        WriteSizePrefixedString(writer, pkg.ContentID);
        WriteSizePrefixedString(writer, pkg.Type switch {
            PKGType.Game => "PS4GD",
            PKGType.Update => "PS4AL",
            PKGType.DLC => "PS4AC",
            _ => "PS4GD"
        });
        writer.Write((ulong)pkg.Size);
        if (pkg.ImageBytes is not null)
        {
            writer.Write((uint)pkg.ImageBytes.Length);
            writer.Write(pkg.ImageBytes);
        }
        else
            writer.Write((uint)0);

        await stream.WriteAsync(buffer.ToArray());
    }

    private static void WriteSizePrefixedString(BinaryWriter writer, string val)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(val + '\0');
        writer.Write((uint)buffer.Length);
        writer.Write(buffer);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        listener.Dispose();
    }
}