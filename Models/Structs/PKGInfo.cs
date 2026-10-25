using System.Drawing;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Ps4PkgSender.Models.Structs;

public enum PKGType
{
    Unknown, Game, Update, DLC
}

public partial class PKGInfo : ObservableObject
{
    public string TitleID {get; set;} = "-";
    public string Title {get; set;} = "-";
    public PKGType Type {get; set;} = PKGType.Unknown;
    public string AppVer {get; set;} = "01.00";
    public string FileName {get; set;} = "-";
    public long Size {get; set;} = 1;
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TransferredSizeGB))]
    private long transferredSize;
    public byte[]? ImageBytes { get; set; }
    public string ContentID {get; set;} = "-";
    [ObservableProperty]
    private bool isTransferring = false;
    public double SizeGB => (double)Size / (1024 * 1024 * 1024);
    public double TransferredSizeGB => (double)TransferredSize / (1024 * 1024 * 1024);
}