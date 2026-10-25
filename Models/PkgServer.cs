using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Ps4PkgSender.Models;

public class PkgServer : IDisposable
{
    private readonly HttpListener server;
    public event Action<string, long>? BytesTransferred;
    public event Action<string>? FileTransferred;
    public event Action<Exception>? TransferError;

    public PkgServer(string localIp, string port)
    {
        server = new HttpListener();
        server.Prefixes.Add($"http://{localIp}:{port}/");
    }

    public void StartServing(Dictionary<string, string> pkgPaths)
    {
        server.Start();
        System.Console.WriteLine("Server Started Serving");
        Task.Run(() => ListenForPs4(pkgPaths));
    }

    async private Task ListenForPs4(Dictionary<string, string> pkgPaths)
    {
        while (server.IsListening)
        {
            HttpListenerContext ctx;
            try
            {
                ctx = await server.GetContextAsync();
            } catch (Exception) when (!server.IsListening)
            {
                break;
            }

            _ = Task.Run(async () => {
                try
                {   
                    await ProcessRequest(ctx, pkgPaths);
                }
                catch (HttpListenerException)
                {
                    System.Console.WriteLine("PS4 Force-Closed the Connection to the PKG Server");
                }
                catch (Exception e)
                {
                    TransferError?.Invoke(e);
                }
            });
        }
    }

    async private Task ProcessRequest(HttpListenerContext ctx, Dictionary<string, string> pkgPaths)
    {
        var response = ctx.Response;
        var request = ctx.Request;
        var requestedPkg = Uri.UnescapeDataString(ctx.Request.Url!.AbsolutePath.TrimStart('/'));

        if (requestedPkg is null)
        {
            response.StatusCode = 404;
            response.OutputStream.Close();
            Console.Write("Request is empty");
            return;
        }
        if (!pkgPaths.ContainsKey(requestedPkg))
        {
            response.StatusCode = 404;
            response.OutputStream.Close();
            Console.Write("Unknown request");
            return;
        }

        string fullPkgPath = pkgPaths[requestedPkg];
        long pkgSize = new FileInfo(fullPkgPath).Length;

        long rangeStart, rangeEnd;
        string? rangeHeader = request.Headers["Range"];
        if (rangeHeader is not null)
            (rangeStart, rangeEnd) = GetStartEndFromRangeHeader(rangeHeader, pkgSize);
        else
        {
            rangeStart = 0;
            rangeEnd = pkgSize - 1;
        }
        System.Console.WriteLine($"Processing requested pkg: {requestedPkg} | {rangeStart}-{rangeEnd}");
        System.Console.WriteLine($"Range Header: {rangeHeader ?? "NONE"}");
        
        long bytesRemaining = rangeEnd - rangeStart + 1;
        response.ContentLength64 = bytesRemaining;
        response.StatusCode = (int)HttpStatusCode.PartialContent;
        response.AddHeader("Content-Range", $"bytes {rangeStart}-{rangeEnd}/{pkgSize}");
        Stream output = ctx.Response.OutputStream;

        using FileStream fs = File.OpenRead(fullPkgPath);
        fs.Position = rangeStart;
        byte[] buffer = new byte[1024 * 64];
        int bytesRead;
        while (bytesRemaining > 0 && (bytesRead = await fs.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, bytesRemaining)))) > 0)
        {
            bytesRemaining -= bytesRead;
            await output.WriteAsync(buffer.AsMemory(0, bytesRead));
            BytesTransferred?.Invoke(requestedPkg, bytesRead);
        }
        output.Close();

        if (bytesRemaining == 0 && rangeEnd == pkgSize - 1)
            FileTransferred?.Invoke(requestedPkg);

        System.Console.WriteLine($"Processed requested pkg: {requestedPkg}");
    }

    private static (long, long) GetStartEndFromRangeHeader(string rangeHeader, long fullSize)
    {
        int startIndex = rangeHeader.IndexOf('=') + 1;
        int endIndex = rangeHeader.IndexOf('-', startIndex);

        string start = rangeHeader[startIndex..endIndex];
        string end = rangeHeader[(endIndex+1)..];

        long startPos, endPos ;
        startPos = long.Parse(start);
        endPos = end.Length > 0 ? long.Parse(end) : fullSize - 1;
        return (startPos, endPos);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        if (server.IsListening)
            server.Stop();
        System.Console.WriteLine("Server Stopped Serving");
    }
}