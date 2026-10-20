namespace Ps4PkgSender.Models.Structs;

public enum PKGType
{
    Unknown, Game, Update, DLC
}
public record PKGInfo
{
    public string TitleID {get; set;} = "-";
    public string Title {get; set;} = "-";
    public PKGType Type {get; set;} = PKGType.Unknown;
    public string AppVer {get; set;} = "01.00";
    public string FileName {get; set;} = "-";
    public long Size {get; set;} = 0;
    public byte[]? ImageBytes { get; set; }
    public string ContentID {get; set;} = "-";
}