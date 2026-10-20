using System;
using System.Text.Json.Serialization;

namespace Ps4PkgSender.Models.Structs;

public record InstallRequest
{
    [JsonPropertyName("type")]
    public string Type { get; } = "direct";

    [JsonPropertyName("packages")]
    public string[] Packages { get; set; } = new string[1];
}