using System;

namespace Ps4PkgSender.Models.Structs;

public struct PKGEntry
{
    public UInt32 filenameOffset;
    public UInt32 flags1;
    public UInt32 flags2;
    public UInt32 fileOffset;
    public UInt32 size;
}