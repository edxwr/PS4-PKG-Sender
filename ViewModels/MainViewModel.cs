using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ps4PkgSender.Models;

namespace Ps4PkgSender.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    public string PcIP { get; } = "";

    [ObservableProperty]
    public partial string Ps4IP { get; set; }

    public ObservableCollection<PKGInfo> Pkgs { get; } = [];
    public Dictionary<string, string> pkgPaths = [];

    private bool CanDelete() => Pkgs.Count > 0;

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

    [RelayCommand(CanExecute = nameof(CanDelete))]
    public async Task StartTransfer()
    {
        using RequestSender sender = new();
        using PkgServer httpServer = new();

        httpServer.StartServing(pkgPaths);
        await sender.ConnectToPs4(Ps4IP);
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