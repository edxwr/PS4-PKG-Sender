using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Dto;
using Ps4PkgSender.Models;
using Ps4PkgSender.Models.Structs;
using Ps4PkgSender.Models.Utils;
using Tmds.DBus.Protocol;

namespace Ps4PkgSender.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string PcIP { get; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTransferCommand))]
    public partial string Ps4IP { get; set; } = "";

    public ObservableCollection<PKGInfo> Pkgs { get; set; } = [];
    private readonly Dictionary<string, string> pkgPaths = [];

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTransferCommand))]

    private partial bool IsTransferring { get; set; } = false;

    public MainViewModel()
    {
        PcIP = Dns.GetHostEntry(String.Empty).AddressList.First(
            ip => ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip)
        ).ToString();

        Pkgs.CollectionChanged += delegate {
            StartTransferCommand.NotifyCanExecuteChanged();
            ClearPKGsCommand.NotifyCanExecuteChanged();
        };
    }

    private bool CanDelete() => Pkgs.Count > 0;
    private bool CanStartTransfer()
    {
        if (IPAddress.TryParse(Ps4IP, out IPAddress? ip) == false)
            return false;
        else if (ip.AddressFamily != AddressFamily.InterNetwork)
            return false;

        return Pkgs.Count > 0 && IsTransferring == false;
    }

    public void AddPKG(string path)
    {
        try
        {   
            if (pkgPaths.TryAdd(Path.GetFileName(path), path))
            {
                Pkgs.Add(PKGUtil.GetInfoFromPath(path));
            }
        } catch (Exception e)
        {
            pkgPaths.Remove(Path.GetFileName(path));
            System.Console.WriteLine(e);
        }
    }

    [RelayCommand]
    public void RemovePKG(PKGInfo pkg)
    {
        if (pkgPaths.Remove(pkg.FileName))
            Pkgs.Remove(pkg);
    }

    [RelayCommand(CanExecute = nameof(CanDelete))]
    public void ClearPKGs()
    {
        pkgPaths.Clear();
        Pkgs.Clear();
    }

    [RelayCommand(CanExecute = nameof(CanStartTransfer))]
    public async Task StartTransfer()
    {
        const string pkgServerPort = "7777";
        const string infoSenderPort = "8888";

        IsTransferring = true;

        using PkgServer pkgServer = new(PcIP, pkgServerPort);
        try
        {   
            pkgServer.StartServing(pkgPaths);
        } catch (Exception e)
        {
            System.Console.WriteLine($"Error starting http server: {e}");
            IsTransferring = false;
            await Dispatcher.UIThread.InvokeAsync(() => ShowPopup("Server Error", "Error while starting HTTP Server"));
            return;
        }

        SortPkgs();

        var pkgMap = Pkgs.ToDictionary(pkg => pkg.FileName);
        var completedPkgs = new HashSet<string>();
        var allTransfersDone = new TaskCompletionSource<bool>();

        foreach (PKGInfo pkg in Pkgs)
            pkg.IsTransferring = true;

        pkgServer.BytesTransferred += (fileName, bytesTransferred) =>
        {
            if (pkgMap.TryGetValue(fileName, out PKGInfo? pkg))
            {
                pkg.TransferredSize = Math.Min(pkg.Size, pkg.TransferredSize + bytesTransferred);
            }
        };

        pkgServer.FileTransferred += (fileName) =>
        {
            if (pkgMap.TryGetValue(fileName, out PKGInfo? pkg))
            {
                if (pkg.TransferredSize >= pkg.Size)
                    lock (completedPkgs)
                    {
                        completedPkgs.Add(fileName);
                        if (completedPkgs.Count == Pkgs.Count)
                            allTransfersDone.TrySetResult(true);
                    }
            }
        };

        pkgServer.TransferError += (ex) =>
        {
            System.Console.WriteLine($"PKG Server Error: {ex}");
            allTransfersDone.TrySetResult(false);
        };

        bool successful = true;
        try 
        {
            using InfoSender infoSender = new(PcIP, infoSenderPort, pkgServerPort, [.. Pkgs]);
            infoSender.StartServing();

            PayloadSender payloadSender = new(Ps4IP);
            await payloadSender.SendPayload("payload.bin", PcIP, infoSenderPort);

            successful = await allTransfersDone.Task;
        } catch (Exception e)
        {
            System.Console.WriteLine($"Error while transferring to PS4: {e}");
            successful = false;
        }

        if (successful)
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowPopup("Transfer Successful", $"Successfully sent all {Pkgs.Count} PKGs"));
            Pkgs.Clear();
            pkgPaths.Clear();
        }
        else
        {
            await Dispatcher.UIThread.InvokeAsync(() => ShowPopup("PS4 Error", $"Error while transferring to the PS4. Transferred {completedPkgs.Count}/{Pkgs.Count} PKGs"));
            foreach (PKGInfo pkg in Pkgs)
            {
                if (pkg.TransferredSize == pkg.Size)
                {
                    pkgPaths.Remove(pkg.FileName);
                    Pkgs.Remove(pkg);
                }
                else
                {
                    pkg.TransferredSize = 0;
                    pkg.IsTransferring = false;
                }
            }
        }

        IsTransferring = false;
        System.Console.WriteLine("Transfer ended");
    }

    private static async Task ShowPopup(string title, string message)
    {
        await MessageBoxManager.GetMessageBoxStandard(
            title,
            message,
            MsBox.Avalonia.Enums.ButtonEnum.Ok
        ).ShowAsync();
    }

    private void SortPkgs()
    {
        var sortedPkgs = Pkgs.OrderBy((pkgInfo) => pkgInfo.Type).ToList();
        Pkgs.Clear();
        foreach (PKGInfo pkg in sortedPkgs)
            Pkgs.Add(pkg);
    }
}