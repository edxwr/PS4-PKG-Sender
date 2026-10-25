using System;
using System.Buffers.Binary;
using System.Collections;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Threading.Tasks;

namespace Ps4PkgSender.Models;

public class PayloadSender(string ps4IP)
{
    private readonly string ps4Ip = ps4IP;

    public async Task SendPayload(string payloadFileName, string pcIp, string infoSenderPort)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var payloadName = assembly.GetManifestResourceNames().FirstOrDefault(
            name => name.EndsWith(payloadFileName)
        ) ?? throw new Exception("Payload not found");
        
        using Stream payloadStream = assembly.GetManifestResourceStream(payloadName)!;
        using MemoryStream ms = new();
        await payloadStream.CopyToAsync(ms);
        byte[] payloadBytes = ms.ToArray();

        try
        {
            await PatchPayload(payloadBytes, pcIp, infoSenderPort);
        } catch (Exception e)
        {
            System.Console.WriteLine(e);
            return;
        }

        using TcpClient client = new();
        await client.ConnectAsync(ps4Ip, 9090);

        using Stream clientStream = client.GetStream();
        await clientStream.WriteAsync(payloadBytes);
    }

    private static async Task PatchPayload(byte[] payloadBytes, string pcIp, string port)
    {
        ReadOnlySpan<byte> defaultValues = [0xB4, 0xB4, 0xB4, 0xB4, 0xB4, 0xB4];

        int index = payloadBytes.AsSpan().IndexOf(defaultValues);
        if (index < 0)
            throw new Exception("Default address values not found");

        byte[] ipBytes = [.. pcIp.Split('.').Select(byte.Parse)];
        ushort portBytes = ushort.Parse(port);

        ipBytes.CopyTo(payloadBytes.AsSpan(index, 4));
        BinaryPrimitives.WriteUInt16BigEndian(payloadBytes.AsSpan(index+4, 2), portBytes);
    }
}