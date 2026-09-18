namespace Ps4PkgSender.Models;

public enum PKGType
{
    None, Game, Update, DLC
}
public record PKGInfo
{
    public string TitleID {get; set;} = "";
    public string Title {get; set;} = "";
    public PKGType Type {get; set;} = PKGType.None;
    public string FileName {get; set;} = "";
    public long Size {get; set;} = 0;
    public byte[]? ImageBytes { get; set; }
}