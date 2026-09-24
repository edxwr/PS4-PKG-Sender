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
    public Dictionary<string, string> pkgPaths = [];

    public MainViewModel()
    {
        PcIP = Dns.GetHostEntry(String.Empty).AddressList.First(
            ip => ip.AddressFamily == AddressFamily.InterNetwork
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
        using RequestSender sender = new();
        using PkgServer httpServer = new(PcIP);

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
            await sender.ConnectToPs4(Ps4IP);
        } catch (Exception e)
        {
            System.Console.WriteLine($"Error connecting to the PS4: {e}");
            httpServer.StopServing();
            return;
        }

        foreach (PKGInfo pkg in Pkgs.ToList().OrderBy((pkgInfo) => pkgInfo.Type))
        {            
            var tcs = new TaskCompletionSource();
            void OnTransferDone(){
                tcs.TrySetResult();
            }
            httpServer.FileTransferred += OnTransferDone;

            await sender.SendInstallCommand(PcIP, pkg.FileName);

            await tcs.Task;

            httpServer.FileTransferred -= OnTransferDone;
        }
        httpServer.StopServing();
    }
}