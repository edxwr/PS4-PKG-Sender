using System.IO;

namespace Ps4PkgSender.Models;

public static class PKGUtil
{
    public static PKGInfo GetPKGFromPath(string path)
    {
        FileStream fs = File.OpenRead(path);
        return new PKGInfo{};
    }
}