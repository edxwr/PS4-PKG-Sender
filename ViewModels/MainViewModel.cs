using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ps4PkgSender.Models;
using Ps4PkgSender.Models.Structs;
using Ps4PkgSender.Models.Utils;

namespace Ps4PkgSender.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string PcIP { get; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartTransferCommand))]
    public partial string Ps4IP { get; set; } = "";

    public ObservableCollection<PKGInfo> Pkgs { get; } = [];
    private readonly Dictionary<string, string> pkgPaths = [];

    public RequestSender? sender;
    private readonly PkgServer httpServer;

    private readonly TaskID taskID = new();
    private bool isPaused = false;
    public string PauseResumeButtonText { get; set; } = "Pause";
    public string PauseResumeButtonColor { get; set; } = "Yellow";
    public long TransferredSize { get; set; }
    public long TotalSize { get; set; }
    public bool IsTransferring { get; set; }
    public PKGInfo CurrentPKG { get; set; } = new();

    public MainViewModel()
    {
        PcIP = Dns.GetHostEntry(String.Empty).AddressList.First(
            ip => ip.AddressFamily == AddressFamily.InterNetwork
        ).ToString();

        Pkgs.CollectionChanged += delegate {
            StartTransferCommand.NotifyCanExecuteChanged();
            ClearPKGsCommand.NotifyCanExecuteChanged();
        };

        httpServer = new(PcIP);
    }

    private bool CanDelete() => Pkgs.Count > 0;
    private bool CanStartTransfer()
    {
        if (IPAddress.TryParse(Ps4IP, out IPAddress? ip) == false)
            return false;
        else if (ip.AddressFamily != AddressFamily.InterNetwork)
            return false;

        return Pkgs.Count > 0;
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
        try
        {   
            httpServer.StartServing(pkgPaths);
        } catch (Exception e)
        {
            System.Console.WriteLine($"Error starting http server: {e}");
            httpServer.StopServing();
            return;
        }

        sender = new(Ps4IP);
        IsTransferring = true;
        isPaused = false;

        foreach (PKGInfo pkg in Pkgs.ToList().OrderBy((pkgInfo) => pkgInfo.Type))
        {
            try
            {
                var tcs = new TaskCompletionSource();
                void OnTransferDone(){
                    tcs.TrySetResult();
                }
                httpServer.FileTransferred += OnTransferDone;

                void OnBytesTransferred(long transferredSize)
                {
                    this.TransferredSize += transferredSize;
                }
                httpServer.BytesTransferred += OnBytesTransferred;

                await sender.SendInstallCommand(PcIP, pkg.FileName);

                taskID.Id = (await sender.GetTaskID(pkg.ContentID, pkg.Type)).Id;
                TotalSize = pkg.Size;
                CurrentPKG = pkg;

                await tcs.Task;
                httpServer.FileTransferred -= OnTransferDone;
                httpServer.BytesTransferred -= OnBytesTransferred;
            } catch (Exception e)
            {
                System.Console.WriteLine($"Error while sending PKG {pkg.FileName}: {e}");
            }
        }

        httpServer.StopServing();

        TransferredSize = TotalSize = 0;
        IsTransferring = false;
        isPaused = false;

        System.Console.WriteLine("All files transferred");
    }

    private bool CanPauseResume() => IsTransferring && sender is not null;
    [RelayCommand(CanExecute = nameof(CanPauseResume))]
    public async Task PauseResumeTransfer()
    {
        if (sender is null)
            return;
        
        if (isPaused)
            await sender.SendResumeCommand(taskID);
        else
            await sender.SendPauseCommand(taskID);

        isPaused = !isPaused;
    }

    private bool CanCancel() => IsTransferring && sender is not null;
    [RelayCommand(CanExecute = nameof(CanCancel))]
    public async Task CancelTransfer()
    {
        if (sender is null)
            return;
        await sender.SendCancelCommand(taskID);
    }
}