using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Ps4PkgSender.Models;

public class PkgServer : IDisposable
{
    private readonly HttpListener server;
    public event Action? FileTransferred;
    public event Action<long>? BytesTransferred;

    public PkgServer(string localIp)
    {
        server = new HttpListener();
        server.Prefixes.Add($"http://{localIp}:7777/");
    }

    public void StartServing(Dictionary<string, string> pkgPaths)
    {
        server.Start();
        System.Console.WriteLine("Server Started Serving");
        Task.Run(() => ListenForPs4(pkgPaths));
    }

    public void StopServing()
    {
        server.Stop();
        System.Console.WriteLine("Server Stopped Serving");
    }

    async private Task ListenForPs4(Dictionary<string, string> pkgPaths)
    {
        while (server.IsListening)
        {
            try
            {   
                var ctx = server.GetContext();
                await ProcessRequest(ctx, pkgPaths);
            } catch
            {
                Console.Write("Eroare la ListenForPs4");
            }
        }
    }

    async private Task ProcessRequest(HttpListenerContext ctx, Dictionary<string, string> pkgPaths)
    {
        var response = ctx.Response;
        var requestedPkg = ctx.Request.RawUrl?.TrimStart('/');
        System.Console.WriteLine($"Processing requested pkg: {requestedPkg}");

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

        response.ContentLength64 = pkgSize;
        response.AddHeader("application", "octet-stream");
        Stream output = ctx.Response.OutputStream;

        using FileStream fs = File.OpenRead(fullPkgPath);
        byte[] buffer = new byte[1024 * 64];
        int bytesRead;
        long totalTransferred = 0;
        while ((bytesRead = await fs.ReadAsync(buffer)) > 0)
        {
            await output.WriteAsync(buffer.AsMemory(0, bytesRead));
            totalTransferred += bytesRead;
            BytesTransferred?.Invoke(totalTransferred);
        }
        output.Close();
        FileTransferred?.Invoke();

        System.Console.WriteLine($"Processed requested pkg: {requestedPkg}");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        server.Close();
    }
}