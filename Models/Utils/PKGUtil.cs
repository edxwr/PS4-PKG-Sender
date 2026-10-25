using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using Ps4PkgSender.Models.Structs;

namespace Ps4PkgSender.Models.Utils;

public static class PKGUtil
{
    // https://www.psdevwiki.com/ps4/PKG_files#Structure
    public static PKGInfo GetInfoFromPath(string path)
    {
        using FileStream fs = File.OpenRead(path);
        using BinaryReader reader = new(fs);

        UInt32 magic = reader.ReadUInt32();
        if (magic != 0x544E437F)
            throw new("Not a valid PS4 PKG");

        Dictionary<UInt32, PKGEntry> pkgEntries = GetEntriesFromFileStream(fs);
        PKGInfo pkgInfo = new();

        if (pkgEntries.TryGetValue(0x1000, out PKGEntry sfoEntry))
        {
            pkgInfo = GetPKGInfoFromSFO(fs, sfoEntry);
            System.Console.WriteLine("Found sfo entry");
        }

        if (pkgEntries.TryGetValue(0x1200, out PKGEntry iconEntry))
        {
            fs.Position = iconEntry.fileOffset;
            pkgInfo.ImageBytes = reader.ReadBytes((int)iconEntry.size);
            System.Console.WriteLine("Found icon entry");
        }  

        pkgInfo.FileName = Path.GetFileName(path);
        pkgInfo.Size = fs.Length;
        
        return pkgInfo;
    }

    private static Dictionary<UInt32, PKGEntry> GetEntriesFromFileStream(FileStream fs)
    {
        BinaryReader reader = new(fs);

        // Header uses big-endian
        fs.Position = 0x10;
        UInt32 entryCount = ReadUInt32BigEndian(reader);

        fs.Position = 0x18;
        UInt32 entryOffset = ReadUInt32BigEndian(reader);

        Dictionary<UInt32, PKGEntry> pkgEntries = [];
        fs.Position = entryOffset;
        for (int i = 0; i < entryCount; ++i)
        {
            pkgEntries[ReadUInt32BigEndian(reader)] = new PKGEntry{
                filenameOffset = ReadUInt32BigEndian(reader),
                flags1 = ReadUInt32BigEndian(reader),
                flags2 = ReadUInt32BigEndian(reader),
                fileOffset = ReadUInt32BigEndian(reader),
                size = ReadUInt32BigEndian(reader),
            };
            reader.ReadUInt64();
        }

        return pkgEntries;
    }

    // https://www.psdevwiki.com/ps3/PARAM.SFO
    private static PKGInfo GetPKGInfoFromSFO(FileStream fs, PKGEntry sfoEntry)
    {
        PKGInfo pkgInfo = new();
        BinaryReader reader = new(fs);

        UInt32 sfoStart = sfoEntry.fileOffset;
        fs.Position = sfoStart + 8;

        UInt32 keyTableStart = reader.ReadUInt32();
        UInt32 dataTableStart = reader.ReadUInt32();
        UInt32 tableEntries = reader.ReadUInt32();

        for (int i = 0; i < tableEntries; ++i)
        {
            fs.Position = sfoStart + 8 + 3 * 4 + i * 16;

            UInt16 keyOffset = reader.ReadUInt16();
            UInt16 dataFormat = reader.ReadUInt16();
            reader.ReadUInt32(); // len
            reader.ReadUInt32(); // maxLen
            UInt32 dataOffset = reader.ReadUInt32();

            if (dataFormat != 0x0204)
                continue;

            string key = "";
            fs.Position = sfoStart + keyTableStart + keyOffset;
            char c;
            while ((c = reader.ReadChar()) != 0)
                key += c;

            string data = "";
            fs.Position = sfoStart + dataTableStart + dataOffset;
            while ((c = reader.ReadChar()) != 0)
                data += c;

            System.Console.WriteLine($"{key} : {data}");

            switch (key)
            {
                case "APP_VER" or "CONTENT_VER":
                    pkgInfo.AppVer = data;
                    break;
                case "CONTENT_ID":
                    pkgInfo.ContentID = data;
                    break;
                case "CATEGORY":
                    pkgInfo.Type = GetPKGTypeFromString(data);
                    break;
                case "TITLE":
                    pkgInfo.Title = data;
                    break;
                case "TITLE_ID":
                    pkgInfo.TitleID = data;
                    break;
            }
        }

        return pkgInfo;
    }

    private static PKGType GetPKGTypeFromString(string data)
    {
        return data switch
        {
            "ac" => PKGType.DLC,
            "gd" => PKGType.Game,
            "gp" => PKGType.Update,
            _ => PKGType.Unknown
        };
    }

    private static UInt32 ReadUInt32BigEndian(BinaryReader reader)
    {
        Span<byte> buffer = stackalloc byte[4];
        reader.Read(buffer);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }
}